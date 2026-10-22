using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCAP.Infrastructure.Persistence.Context;

namespace OCAP.Security.Infrastructure.Services;

/// <summary>
/// Alta del primer admin desde el entorno, para despliegues automatizados (CI, demos efímeras).
/// Es opcional: solo actúa si se definen <c>Bootstrap:AdminEmail</c> y <c>Bootstrap:AdminPassword</c>.
/// Sin ellos no hay credenciales por defecto y el primer acceso se crea con la configuración inicial (/setup).
/// </summary>
public sealed class BootstrapAdminHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BootstrapAdminHostedService> _logger;

    public BootstrapAdminHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<BootstrapAdminHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue("Bootstrap:Enabled", true)) return;

        var email = _configuration["Bootstrap:AdminEmail"];
        var password = _configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OCAPDbContext>();
        if (!db.Database.IsRelational())
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        var provisioner = scope.ServiceProvider.GetRequiredService<InitialAdminProvisioner>();
        if (await provisioner.HasUsersAsync(cancellationToken)) return;

        await provisioner.CreateAsync(
            new InitialAdminRequest(
                email,
                password,
                "Administrator",
                _configuration["Bootstrap:TenantName"] ?? "OCAP",
                _configuration["Bootstrap:TenantSlug"] ?? "default"),
            cancellationToken);

        _logger.LogInformation("Administrador inicial creado desde el entorno para {Email}.", email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
