using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

/// <summary>Administración de suscripciones e historial de entregas de Webhooks (CAP-12).</summary>
[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ITenantContext _tenantContext;
    private readonly ISecurityAuditService _auditService;

    public WebhooksController(
        IWebhookService webhookService,
        ITenantContext tenantContext,
        ISecurityAuditService auditService)
    {
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    private Guid RequireTenantId()
    {
        if (_tenantContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Contexto de tenant requerido.");
        }

        return _tenantContext.TenantId;
    }

    [HttpGet]
    public async Task<IActionResult> GetWebhooks(CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var subscriptions = await _webhookService.GetSubscriptionsForTenantAsync(tenantId, cancellationToken);
        return Ok(subscriptions);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookRequestDto request, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        if (string.IsNullOrWhiteSpace(request.Secret))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Secret requerido",
                Detail = "Debe proporcionar un secret HMAC para la suscripción."
            });
        }

        var subscription = await _webhookService.CreateSubscriptionAsync(
            tenantId, request.Name, request.TargetUrl, request.Secret, request.SubscribedEvents, cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, Guid.NewGuid(), "Webhook.Created", $"Webhook {request.Name} registrado", "WebhooksController", true, cancellationToken);
        return CreatedAtAction(nameof(GetWebhooks), new { id = subscription.Id }, subscription);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWebhook(Guid id, [FromBody] UpdateWebhookRequestDto request, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var updated = await _webhookService.UpdateSubscriptionAsync(
            id, tenantId, request.Name, request.TargetUrl, request.Secret, request.SubscribedEvents, request.IsActive, cancellationToken);

        if (updated == null) return NotFound(new ProblemDetails { Title = "No encontrado", Detail = "Suscripción de Webhook no encontrada.", Status = 404 });
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebhook(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var success = await _webhookService.DeleteSubscriptionAsync(id, tenantId, cancellationToken);
        if (!success) return NotFound(new ProblemDetails { Title = "No encontrado", Detail = "Suscripción no encontrada.", Status = 404 });

        return Ok(new { message = "Webhook eliminado correctamente." });
    }

    [HttpGet("{id}/deliveries")]
    public async Task<IActionResult> GetDeliveryHistory(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var logs = await _webhookService.GetDeliveryHistoryAsync(id, tenantId, cancellationToken);
        return Ok(logs);
    }
}

public class CreateWebhookRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public List<string> SubscribedEvents { get; set; } = new();
}

public class UpdateWebhookRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public List<string> SubscribedEvents { get; set; } = new();
    public bool IsActive { get; set; } = true;
}
