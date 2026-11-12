using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Core.Entities;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Infrastructure.Services;

/// <summary>Ajustes de la instalación definidos en el panel; un valor null usa el de la configuración del servidor.</summary>
public sealed record SystemSettingsSnapshot(
    bool? RateLimitingEnabled,
    int? RequestsPerMinute,
    int? AuthAttemptsPerMinute,
    int? WidgetMessagesPerMinute,
    int? WidgetReadsPerMinute,
    int? SessionMinutes,
    IReadOnlyList<string> AllowedOrigins)
{
    public static readonly SystemSettingsSnapshot Empty = new(null, null, null, null, null, null, []);
}

/// <summary>
/// Copia en memoria de los ajustes del sistema. El limitador de peticiones y CORS deciden en cada petición sin poder
/// esperar a la base, así que leen de aquí; <see cref="SystemSettingsRefreshService"/> la mantiene al día.
/// </summary>
public sealed class SystemSettingsStore
{
    private readonly IServiceScopeFactory _scopes;
    private volatile SystemSettingsSnapshot _current = SystemSettingsSnapshot.Empty;

    public SystemSettingsStore(IServiceScopeFactory scopes) => _scopes = scopes;

    public SystemSettingsSnapshot Current => _current;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        var row = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        _current = row is null ? SystemSettingsSnapshot.Empty : ToSnapshot(row);
    }

    public async Task SaveAsync(SystemSettingsSnapshot settings, CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        var row = await db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new SystemSettings();
            db.SystemSettings.Add(row);
        }

        row.RateLimitingEnabled = settings.RateLimitingEnabled;
        row.RequestsPerMinute = settings.RequestsPerMinute;
        row.AuthAttemptsPerMinute = settings.AuthAttemptsPerMinute;
        row.WidgetMessagesPerMinute = settings.WidgetMessagesPerMinute;
        row.WidgetReadsPerMinute = settings.WidgetReadsPerMinute;
        row.SessionMinutes = settings.SessionMinutes;
        row.AllowedOrigins = [.. settings.AllowedOrigins];
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        _current = ToSnapshot(row);
    }

    private static SystemSettingsSnapshot ToSnapshot(SystemSettings row) => new(
        row.RateLimitingEnabled,
        row.RequestsPerMinute,
        row.AuthAttemptsPerMinute,
        row.WidgetMessagesPerMinute,
        row.WidgetReadsPerMinute,
        row.SessionMinutes,
        [.. row.AllowedOrigins]);
}

/// <summary>Carga los ajustes al arrancar y los relee cada minuto, por si otra instancia de la API los cambió.</summary>
public sealed class SystemSettingsRefreshService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly SystemSettingsStore _store;
    private readonly ILogger<SystemSettingsRefreshService> _logger;

    public SystemSettingsRefreshService(SystemSettingsStore store, ILogger<SystemSettingsRefreshService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await TryRefreshAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await TryRefreshAsync(stoppingToken);
        }
    }

    // Si la base falla se conservan los últimos ajustes conocidos: perderlos quitaría límites que alguien puso.
    private async Task TryRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _store.RefreshAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "No se pudieron leer los ajustes del sistema; siguen los anteriores.");
        }
    }
}
