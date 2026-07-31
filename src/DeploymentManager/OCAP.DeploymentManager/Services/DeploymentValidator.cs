using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using OCAP.DeploymentManager.Models;

namespace OCAP.DeploymentManager.Services;

/// <summary>
/// Validaciones reales de infraestructura previa al despliegue (sin simulaciones).
/// </summary>
public class DeploymentValidator
{
    public (bool IsValid, List<string> Errors) Validate(DeploymentConfiguration config)
    {
        var errors = new List<string>();

        if (config == null)
        {
            errors.Add("La configuración de despliegue no puede ser nula.");
            return (false, errors);
        }

        if (string.IsNullOrWhiteSpace(config.PostgresHost)) errors.Add("El Host de PostgreSQL es obligatorio.");
        if (string.IsNullOrWhiteSpace(config.PostgresDbName)) errors.Add("El nombre de la base de datos es obligatorio.");
        if (string.IsNullOrWhiteSpace(config.PostgresUsername)) errors.Add("El usuario de PostgreSQL es obligatorio.");
        if (string.IsNullOrWhiteSpace(config.PostgresPassword)) errors.Add("La contraseña de PostgreSQL es obligatoria.");

        if (config.EnableWhatsApp)
        {
            if (string.IsNullOrWhiteSpace(config.EvolutionApiUrl)) errors.Add("La URL de Evolution API es obligatoria.");
            if (string.IsNullOrWhiteSpace(config.EvolutionApiKey)) errors.Add("La API Key de Evolution API es obligatoria.");
        }

        if (config.EnableTelegram && string.IsNullOrWhiteSpace(config.TelegramBotToken))
            errors.Add("El bot token de Telegram es obligatorio cuando Telegram está activo.");

        if (config.EnableGoogleWorkspace)
        {
            if (string.IsNullOrWhiteSpace(config.GoogleClientId)) errors.Add("Google Client ID es obligatorio.");
            if (string.IsNullOrWhiteSpace(config.GoogleClientSecret)) errors.Add("Google Client Secret es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(config.BootstrapAdminEmail)) errors.Add("El email admin es obligatorio.");
        if (string.IsNullOrWhiteSpace(config.BootstrapAdminPassword) || config.BootstrapAdminPassword.Length < 10)
            errors.Add("La contraseña admin debe tener al menos 10 caracteres.");

        if (config.Target == DeploymentTarget.Local)
        {
            if (config.FrontendHostPort is < 1 or > 65535) errors.Add("Puerto frontend inválido.");
            if (config.ApiHostPort is < 1 or > 65535) errors.Add("Puerto API inválido.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(config.PublicApiUrl)) errors.Add("PublicApiUrl es obligatoria en modo Web.");
            if (string.IsNullOrWhiteSpace(config.PublicPanelUrl)) errors.Add("PublicPanelUrl es obligatoria en modo Web.");
        }

        if (config.JwtSecretKey.Length < 16) errors.Add("La clave secreta JWT debe tener al menos 16 caracteres.");

        return (errors.Count == 0, errors);
    }

    public async Task<DeploymentValidationReport> ValidateInfrastructureAsync(
        DeploymentConfiguration config,
        string? composeFilePath = null,
        CancellationToken cancellationToken = default)
    {
        var report = new DeploymentValidationReport();
        var (isValid, errors) = Validate(config);
        report.ConfigValid = isValid;
        report.ConfigErrors.AddRange(errors);

        report.DockerAvailable = await CommandExistsAsync("docker", cancellationToken);
        report.ComposeAvailable = await CommandExistsAsync("docker", cancellationToken)
                                  && await DockerComposeAvailableAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(composeFilePath))
        {
            report.ComposeFileExists = File.Exists(composeFilePath);
        }

        report.PostgresReachable = await TcpReachableAsync(config.PostgresHost, config.PostgresPort, cancellationToken);
        report.RabbitMqReachable = await TcpReachableAsync(config.RabbitMqHost, config.RabbitMqPort, cancellationToken);
        report.NatsReachable = await TcpReachableAsync(config.NatsHost, config.NatsPort, cancellationToken);
        report.JwtSecretConfigured = config.JwtSecretKey.Length >= 32;
        report.StoragePathWritable = EnsureStorageWritable(config.StorageRootPath);
        report.TelemetryEndpointReachable = string.IsNullOrWhiteSpace(config.OtlpEndpoint)
            || await HttpReachableAsync(config.OtlpEndpoint, cancellationToken);

        if (!string.IsNullOrWhiteSpace(config.ApiHealthUrl))
        {
            report.ApiHealthOk = await HttpReachableAsync(config.ApiHealthUrl, cancellationToken);
        }

        report.LicenseKeyPresent = !string.IsNullOrWhiteSpace(config.LicenseKey);
        report.IsReady = report.ConfigValid
                         && report.DockerAvailable
                         && report.ComposeAvailable
                         && (composeFilePath is null || report.ComposeFileExists);

        return report;
    }

    private static bool EnsureStorageWritable(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, $".ocap-write-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TcpReachableAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, timeout.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> HttpReachableAsync(string url, CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(url, ct);
            return response.IsSuccessStatusCode || (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> CommandExistsAsync(string command, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var process = Process.Start(psi);
            if (process is null) return false;
            await process.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> DockerComposeAvailableAsync(CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var process = Process.Start(psi);
            if (process is null) return false;
            await process.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class DeploymentValidationReport
{
    public bool ConfigValid { get; set; }
    public List<string> ConfigErrors { get; } = new();
    public bool DockerAvailable { get; set; }
    public bool ComposeAvailable { get; set; }
    public bool ComposeFileExists { get; set; }
    public bool PostgresReachable { get; set; }
    public bool RabbitMqReachable { get; set; }
    public bool NatsReachable { get; set; }
    public bool JwtSecretConfigured { get; set; }
    public bool StoragePathWritable { get; set; }
    public bool TelemetryEndpointReachable { get; set; }
    public bool ApiHealthOk { get; set; }
    public bool LicenseKeyPresent { get; set; }
    public bool IsReady { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}
