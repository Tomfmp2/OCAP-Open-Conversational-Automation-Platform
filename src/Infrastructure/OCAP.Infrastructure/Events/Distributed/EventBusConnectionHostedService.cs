using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

/// <summary>
/// Conecta el transporte al arranque y reconecta en fallo.
/// </summary>
public sealed class EventBusConnectionHostedService : BackgroundService
{
    private readonly IEventTransport _transport;
    private readonly EventBusOptions _options;
    private readonly ILogger<EventBusConnectionHostedService> _logger;

    public EventBusConnectionHostedService(
        IEventTransport transport,
        IOptions<EventBusOptions> options,
        ILogger<EventBusConnectionHostedService> logger)
    {
        _transport = transport;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var healthy = await _transport.HealthCheckAsync(stoppingToken);
                if (!healthy)
                {
                    _logger.LogInformation("Connecting event transport {Provider}...", _transport.ProviderName);
                    await _transport.ConnectAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.ReconnectDelaySeconds)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event transport reconnect failed; retrying");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        try
        {
            await _transport.DisconnectAsync(CancellationToken.None);
        }
        catch
        {
            // ignore shutdown errors
        }
    }
}
