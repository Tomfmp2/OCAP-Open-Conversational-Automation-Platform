using Microsoft.EntityFrameworkCore;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Api.Services;

/// <summary>
/// Apagado gradual de un canal: al pulsar «Apagar» deja de aceptar chats nuevos, pero los que estaban en curso siguen
/// hasta que pasan <see cref="IdleWindow"/> sin mensajes. Cuando ya no queda ninguno, el canal se desactiva del todo
/// hasta que alguien lo vuelva a encender.
/// </summary>
public static class ChannelShutdown
{
    /// <summary>Metadato de la conexión con el momento (UTC, ISO 8601) en que se pidió apagarla.</summary>
    public const string ShuttingDownSinceKey = "ShuttingDownSince";

    public static readonly TimeSpan IdleWindow = TimeSpan.FromMinutes(5);

    public static bool IsShuttingDown(IReadOnlyDictionary<string, string> metadata) => metadata.ContainsKey(ShuttingDownSinceKey);

    /// <summary>Un chat sigue en curso si tuvo actividad dentro de la ventana.</summary>
    public static bool IsOngoing(DateTime lastActivityUtc, DateTime nowUtc) => nowUtc - lastActivityUtc < IdleWindow;
}

/// <summary>Cada minuto desactiva los canales en apagado que ya no tienen chats en curso.</summary>
public sealed class ChannelShutdownBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChannelShutdownBackgroundService> _logger;

    public ChannelShutdownBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ChannelShutdownBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CompleteShutdownsAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "No se pudo revisar el apagado de canales.");
            }
            await Task.Delay(Interval, stoppingToken);
        }
    }

    internal async Task CompleteShutdownsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        var connections = scope.ServiceProvider.GetRequiredService<IChannelConnectionManager>();
        var since = nowUtc - ChannelShutdown.IdleWindow;

        var candidates = (await db.ChannelConnections.IgnoreQueryFilters().Where(c => c.Enabled).ToListAsync(cancellationToken))
            .Where(c => ChannelShutdown.IsShuttingDown(c.ConfigurationMetadata))
            .ToList();
        foreach (var connection in candidates)
        {
            var ongoing = await db.Conversations.IgnoreQueryFilters()
                .AnyAsync(c => c.ChannelConnectionId == connection.Id && c.LastActivityAt > since, cancellationToken);
            if (ongoing) continue;

            await connections.DisableChannelAsync(connection.TenantId, connection.Id, cancellationToken);
            await connections.SetMetadataValueAsync(connection.TenantId, connection.Id, ChannelShutdown.ShuttingDownSinceKey, null, cancellationToken);
            _logger.LogInformation("Canal {ConnectionId} apagado: no quedan chats en curso.", connection.Id);
        }
    }
}
