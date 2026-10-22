using System.Security.Cryptography;
using System.Text;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Api.Setup;

/// <summary>
/// Código de un solo uso que protege la configuración inicial. Mientras la base no tenga usuarios, la API
/// lo imprime en sus logs: solo quien administra el servidor puede crear el primer admin (patrón Grafana/Jenkins).
/// Vive en memoria: al reiniciar sin usuarios se emite uno nuevo.
/// </summary>
public sealed class SetupCodeService : IHostedService
{
    // Sin 0/O ni 1/I/L: el código se copia a mano desde la consola.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int GroupLength = 4;
    private const int Groups = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SetupCodeService> _logger;
    private readonly Lock _gate = new();
    private string? _code;

    /// <summary>Serializa la creación del admin: dos envíos simultáneos no pueden crear dos cuentas.</summary>
    public SemaphoreSlim CompletionLock { get; } = new(1, 1);

    public SetupCodeService(IServiceScopeFactory scopeFactory, ILogger<SetupCodeService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Corre después del bootstrap por entorno: si ese creó el admin, no hay nada que proteger.
        if (await IsPendingAsync(cancellationToken))
            EnsureCode();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>La configuración inicial está pendiente mientras no exista ningún usuario.</summary>
    public async Task<bool> IsPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<InitialAdminProvisioner>();
        return !await provisioner.HasUsersAsync(cancellationToken);
    }

    /// <summary>Devuelve el código vigente o emite uno (p. ej. si se borraron todos los usuarios con la API en marcha).</summary>
    public void EnsureCode()
    {
        lock (_gate)
        {
            if (_code is not null) return;
            _code = Generate();
        }

        _logger.LogWarning(
            "Configuración inicial pendiente. Abre el panel en /setup e ingresa este código: {SetupCode}",
            _code);
    }

    public bool Verify(string? candidate)
    {
        string? current;
        lock (_gate) current = _code;
        if (current is null || string.IsNullOrWhiteSpace(candidate)) return false;

        var normalized = Normalize(candidate);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(normalized),
            Encoding.ASCII.GetBytes(Normalize(current)));
    }

    /// <summary>Solo para pruebas de integración: en ejecución, el código se lee en los logs.</summary>
    internal string? CurrentCode
    {
        get { lock (_gate) return _code; }
    }

    public void Invalidate()
    {
        lock (_gate) _code = null;
    }

    /// <summary>El usuario puede escribirlo con o sin guiones y en minúsculas.</summary>
    private static string Normalize(string code) =>
        new(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string Generate()
    {
        var groups = Enumerable.Range(0, Groups)
            .Select(_ => new string(Enumerable.Range(0, GroupLength)
                .Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)])
                .ToArray()));
        return string.Join('-', groups);
    }
}
