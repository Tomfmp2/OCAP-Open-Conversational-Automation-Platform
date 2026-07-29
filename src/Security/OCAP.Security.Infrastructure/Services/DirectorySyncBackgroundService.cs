using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

// Servicio en segundo plano para sincronización periódica programada de directorios (CAP-19).
public class DirectorySyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DirectorySyncBackgroundService> _logger;

    public DirectorySyncBackgroundService(IServiceProvider serviceProvider, ILogger<DirectorySyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando servicio en segundo plano DirectorySyncBackgroundService (CAP-19)...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var syncEngine = scope.ServiceProvider.GetService<IDirectorySyncEngine>();
                if (syncEngine != null)
                {
                    _logger.LogDebug("Comprobando tareas de sincronización de directorio programadas...");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la ejecución periódica del motor de sincronización de directorio.");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
