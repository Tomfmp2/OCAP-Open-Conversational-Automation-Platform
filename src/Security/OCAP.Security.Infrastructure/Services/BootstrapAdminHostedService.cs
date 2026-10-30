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
        if (!IsStrong(password))
        {
            // Una contraseña débil o de ejemplo por entorno sería una puerta trasera: se exige la configuración inicial.
            _logger.LogError(
                "Bootstrap:AdminPassword no cumple la política (mínimo {Min} caracteres con letras y números). " +
                "No se creó el administrador desde el entorno.", MinPasswordLength);
            return;
        }

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
                string.IsNullOrWhiteSpace(_configuration["Bootstrap:AdminName"]) ? "Administrator" : _configuration["Bootstrap:AdminName"]!.Trim(),
                string.IsNullOrWhiteSpace(_configuration["Bootstrap:TenantName"]) ? "OCAP" : _configuration["Bootstrap:TenantName"]!.Trim(),
                string.IsNullOrWhiteSpace(_configuration["Bootstrap:TenantSlug"]) ? "default" : _configuration["Bootstrap:TenantSlug"]!.Trim()),
            cancellationToken);

        _logger.LogWarning(
            "Administrador inicial creado desde el entorno para {Email}. Quita BOOTSTRAP_ADMIN_PASSWORD del entorno una vez creado.",
            email);
    }

    /// <summary>La misma política que la configuración inicial (SetupRequestValidator).</summary>
    public const int MinPasswordLength = 12;

    private static bool IsStrong(string password) =>
        password.Length >= MinPasswordLength && password.Any(char.IsLetter) && password.Any(char.IsDigit);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
