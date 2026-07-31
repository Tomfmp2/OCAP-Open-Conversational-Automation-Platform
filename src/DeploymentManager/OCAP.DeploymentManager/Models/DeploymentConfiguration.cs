namespace OCAP.DeploymentManager.Models;

public enum InstallationMode
{
    LocalDevelopment = 1,
    PersonalServer = 2,
    EnterpriseServer = 3
}

public enum DeploymentTarget
{
    Local = 1,
    Web = 2
}

public class DeploymentConfiguration
{
    public InstallationMode Mode { get; set; } = InstallationMode.LocalDevelopment;
    public DeploymentTarget Target { get; set; } = DeploymentTarget.Local;

    public int FrontendHostPort { get; set; } = 3000;
    public int ApiHostPort { get; set; } = 5000;
    public string PublicApiUrl { get; set; } = "http://localhost:5000";
    public string PublicPanelUrl { get; set; } = "http://localhost:3000";

    public string PostgresHost { get; set; } = "localhost";
    public int PostgresPort { get; set; } = 5433;
    public string PostgresDbName { get; set; } = "ocap_db";
    public string PostgresUsername { get; set; } = "ocap_user";
    public string PostgresPassword { get; set; } = "OcapSecurePass2026!";

    public string RabbitMqHost { get; set; } = "localhost";
    public int RabbitMqPort { get; set; } = 5672;
    public string RabbitMqUser { get; set; } = "ocap";
    public string RabbitMqPassword { get; set; } = "OcapRabbit2026!";
    public string NatsHost { get; set; } = "localhost";
    public int NatsPort { get; set; } = 4222;

    public string BootstrapAdminEmail { get; set; } = "admin@ocap.io";
    public string BootstrapAdminPassword { get; set; } = "ChangeMe_Admin_2026!";
    public string BootstrapTenantName { get; set; } = "OCAP Default";
    public string BootstrapTenantSlug { get; set; } = "default";

    public bool EnableWhatsApp { get; set; }
    public string EvolutionApiUrl { get; set; } = "http://localhost:8088";
    public string EvolutionApiKey { get; set; } = "EvolutionSecretApiKey";

    public bool EnableTelegram { get; set; }
    public string TelegramBotToken { get; set; } = string.Empty;

    public bool EnableGoogleWorkspace { get; set; } = true;
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;
    public string GoogleRedirectUri { get; set; } = string.Empty;

    public string AiProvider { get; set; } = "OpenAI";
    public string AiApiKey { get; set; } = string.Empty;
    public string AiModelName { get; set; } = "gpt-4o";
    public string AiBaseUrl { get; set; } = string.Empty;

    public string JwtSecretKey { get; set; } = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    public string VaultMasterKey { get; set; } = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    public string StorageRootPath { get; set; } = "./storage";
    public string EventBusProvider { get; set; } = "RabbitMQ";
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public string ApiHealthUrl { get; set; } = "http://localhost:5000/health/ready";
    public string LicenseKey { get; set; } = "DEV-LICENSE";
    public string ComposeFilePath { get; set; } = "docker-compose.yml";

    public string ResolvePublicApiUrl()
    {
        if (Target == DeploymentTarget.Web && !string.IsNullOrWhiteSpace(PublicApiUrl))
            return PublicApiUrl.TrimEnd('/');
        return $"http://localhost:{ApiHostPort}";
    }

    public string ResolvePublicPanelUrl()
    {
        if (Target == DeploymentTarget.Web && !string.IsNullOrWhiteSpace(PublicPanelUrl))
            return PublicPanelUrl.TrimEnd('/');
        return $"http://localhost:{FrontendHostPort}";
    }

    public string ResolveGoogleRedirectUri()
    {
        if (!string.IsNullOrWhiteSpace(GoogleRedirectUri))
            return GoogleRedirectUri.Trim();
        return $"{ResolvePublicApiUrl()}/api/integrations/Google/connect";
    }
}
