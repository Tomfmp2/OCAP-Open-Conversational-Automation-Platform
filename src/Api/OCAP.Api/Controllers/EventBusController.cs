using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OCAP.Core.Events.Distributed;
using OCAP.Infrastructure.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/eventbus")]
[Authorize]
public class EventBusController : ControllerBase
{
    private readonly IEventTransport _transport;
    private readonly IMessageDeadLetterHandler _deadLetterHandler;
    private readonly OCAPDbContext _dbContext;
    private readonly EventBusOptions _options;

    public EventBusController(
        IEventTransport transport,
        IMessageDeadLetterHandler deadLetterHandler,
        OCAPDbContext dbContext,
        IOptions<EventBusOptions> options)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _deadLetterHandler = deadLetterHandler ?? throw new ArgumentNullException(nameof(deadLetterHandler));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options.Value;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var isHealthy = await _transport.HealthCheckAsync(cancellationToken);
        return Ok(new
        {
            status = isHealthy ? "Healthy" : "Degraded",
            activeProvider = _transport.ProviderName,
            clusterNodeId = Environment.MachineName,
            immediateDispatch = _options.ImmediateDispatch,
            enableOutbox = _options.EnableOutbox,
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        var pendingOutboxCount = await _dbContext.DistributedOutboxMessages.CountAsync(m => m.Status == "Pending", cancellationToken);
        var totalProcessedOutbox = await _dbContext.DistributedOutboxMessages.CountAsync(m => m.Status == "Processed", cancellationToken);
        var failedOutbox = await _dbContext.DistributedOutboxMessages.CountAsync(m => m.Status == "Failed", cancellationToken);
        var deadLetterCount = await _dbContext.DeadLetterMessages.CountAsync(m => !m.Replayed, cancellationToken);
        var inboxCount = await _dbContext.InboxMessages.CountAsync(cancellationToken);

        return Ok(new
        {
            pendingOutboxCount,
            totalProcessedOutbox,
            failedOutbox,
            deadLetterCount,
            inboxCount,
            activeProvider = _transport.ProviderName
        });
    }

    [HttpGet("retries")]
    public IActionResult GetRetries()
    {
        return Ok(new
        {
            maxRetries = _options.MaxRetries,
            backoffStrategy = "ExponentialBackoff",
            poisonMessageThreshold = _options.MaxRetries,
            consumerGroup = _options.ConsumerGroup
        });
    }

    [HttpGet("deadletters")]
    public async Task<IActionResult> GetDeadLetters([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var list = await _deadLetterHandler.GetDeadLettersAsync(tenantId, cancellationToken);
        return Ok(list);
    }

    [HttpPost("deadletters/retry")]
    public async Task<IActionResult> ReplayDeadLetter([FromQuery] Guid deadLetterId, CancellationToken cancellationToken)
    {
        var success = await _deadLetterHandler.ReplayDeadLetterAsync(deadLetterId, cancellationToken);
        if (!success) return NotFound(new { error = "Mensaje muerto no encontrado." });

        return Ok(new { message = "Mensaje reencolado en outbox para reintento." });
    }

    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections(CancellationToken cancellationToken)
    {
        var isConnected = await _transport.HealthCheckAsync(cancellationToken);
        return Ok(new
        {
            provider = _transport.ProviderName,
            isConnected,
            nodeId = Environment.MachineName
        });
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        return Ok(new[]
        {
            new { name = "InMemory", status = "Available", type = "Local (Development/Testing)" },
            new { name = "RabbitMQ", status = "Available", type = "AMQP 0-9-1" },
            new { name = "NATS", status = "Available", type = "JetStream" }
        });
    }
}
