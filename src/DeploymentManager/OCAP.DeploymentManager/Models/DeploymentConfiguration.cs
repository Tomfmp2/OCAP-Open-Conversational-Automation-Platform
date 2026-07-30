namespace OCAP.DeploymentManager.Models;

public enum InstallationMode
{
    LocalDevelopment = 1,
    PersonalServer = 2,
    EnterpriseServer = 3
}

public class DeploymentConfiguration
{
    public InstallationMode Mode { get; set; } = InstallationMode.LocalDevelopment;

    public string PostgresHost { get; set; } = "localhost";
    public int PostgresPort { get; set; } = 5433;
    public string PostgresDbName { get; set; } = "ocap_db";
    public string PostgresUsername { get; set; } = "ocap_user";
    public string PostgresPassword { get; set; } = "OcapSecurePass2026!";

    public string RabbitMqHost { get; set; } = "localhost";
    public int RabbitMqPort { get; set; } = 5672;
    public string NatsHost { get; set; } = "localhost";
    public int NatsPort { get; set; } = 4222;

    public bool EnableWhatsApp { get; set; } = true;
    public string EvolutionApiUrl { get; set; } = "http://localhost:8080";
    public string EvolutionApiKey { get; set; } = "EvolutionSecretApiKey";

    public bool EnableGoogleWorkspace { get; set; } = true;
    public string GoogleClientId { get; set; } = "your-google-client-id.apps.googleusercontent.com";
    public string GoogleClientSecret { get; set; } = "your-google-client-secret";
    public string GoogleRedirectUri { get; set; } = "http://localhost:5000/api/auth/google/callback";

    public string JwtSecretKey { get; set; } = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    public string StorageRootPath { get; set; } = "./storage";
    public string EventBusProvider { get; set; } = "RabbitMQ";
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public string ApiHealthUrl { get; set; } = "http://localhost:5000/health/ready";
    public string LicenseKey { get; set; } = "DEV-LICENSE";
    public string ComposeFilePath { get; set; } = "docker-compose.yml";
}
