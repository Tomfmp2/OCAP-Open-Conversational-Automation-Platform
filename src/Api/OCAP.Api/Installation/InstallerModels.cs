namespace OCAP.Api.Installation;

public enum InstallerDeploymentTarget
{
    Local = 1,
    Web = 2
}

public sealed class InstallerSetupRequest
{
    public string Target { get; set; } = "Local";

    public int FrontendHostPort { get; set; } = 3000;
    public int ApiHostPort { get; set; } = 5000;
    public string? PublicApiUrl { get; set; }
    public string? PublicPanelUrl { get; set; }

    public string PostgresHost { get; set; } = "localhost";
    public int PostgresPort { get; set; } = 5433;
    public string PostgresDbName { get; set; } = "ocap_db";
    public string PostgresUsername { get; set; } = "ocap_user";
    public string PostgresPassword { get; set; } = string.Empty;

    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string TenantName { get; set; } = "OCAP Default";
    public string TenantSlug { get; set; } = "default";

    public bool EnableGoogleWorkspace { get; set; } = true;
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }
    public string? GoogleRedirectUri { get; set; }

    public string AiProvider { get; set; } = "OpenAI";
    public string? AiApiKey { get; set; }
    public string AiModelName { get; set; } = "gpt-4o";
    public string? AiBaseUrl { get; set; }

    public bool EnableWhatsApp { get; set; }
    public string? EvolutionApiUrl { get; set; }
    public string? EvolutionApiKey { get; set; }

    public bool EnableTelegram { get; set; }
    public string? TelegramBotToken { get; set; }

    public string? JwtSecretKey { get; set; }
    public string? VaultMasterKey { get; set; }
}

public sealed class InstallerStatusResponse
{
    public bool Completed { get; set; }
    public string Target { get; set; } = "Local";
    public int? FrontendHostPort { get; set; }
    public int? ApiHostPort { get; set; }
    public string? PublicApiUrl { get; set; }
    public string? PublicPanelUrl { get; set; }
    public bool HasAdminUsers { get; set; }
    public bool GoogleConfigured { get; set; }
    public bool AiConfigured { get; set; }
    public string ConfigPath { get; set; } = string.Empty;
}

public sealed class InstallerSetupResponse
{
    public bool Success { get; set; }
    public bool RequiresRestart { get; set; }
    public bool AdminCreated { get; set; }
    public bool DotEnvWritten { get; set; }
    public string Message { get; set; } = string.Empty;
    public string EnvFilePreview { get; set; } = string.Empty;
    public string EnvFilePath { get; set; } = string.Empty;
    public string DotEnvPath { get; set; } = string.Empty;
    public string RestartHint { get; set; } = string.Empty;
    public InstallerStatusResponse Status { get; set; } = new();
}
