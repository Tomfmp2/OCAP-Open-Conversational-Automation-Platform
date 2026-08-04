namespace OCAP.Api.Installation;

/// <summary>
/// Dev = local sin Docker (ocap-dev, :5229, UseInMemory).
/// Local = Docker Compose (:3000/:5000).
/// Web = despliegue con URLs públicas.
/// </summary>
public enum InstallerDeploymentTarget
{
    Dev = 0,
    Local = 1,
    Web = 2
}

public sealed class InstallerSetupRequest
{
    public string Target { get; set; } = "Dev";

    public int FrontendHostPort { get; set; } = 3000;
    public int ApiHostPort { get; set; } = 5229;
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

    public bool EnableGoogleWorkspace { get; set; }
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }
    public string? GoogleRedirectUri { get; set; }

    public string AiProvider { get; set; } = "Gemini";
    public string? AiApiKey { get; set; }
    public string AiModelName { get; set; } = "gemini-3.5-flash";
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
    public string Target { get; set; } = "Dev";
    public int? FrontendHostPort { get; set; }
    public int? ApiHostPort { get; set; }
    public string? PublicApiUrl { get; set; }
    public string? PublicPanelUrl { get; set; }
    public bool HasAdminUsers { get; set; }
    public bool GoogleConfigured { get; set; }
    public bool AiConfigured { get; set; }
    public string ConfigPath { get; set; } = string.Empty;
    /// <summary>True si el entorno permite setup anónimo (primera instalación).</summary>
    public bool AllowsAnonymousSetup { get; set; }
}

public sealed class InstallerSetupResponse
{
    public bool Success { get; set; }
    public bool RequiresRestart { get; set; }
    public bool AdminCreated { get; set; }
    public bool AdminUpdated { get; set; }
    public bool DotEnvWritten { get; set; }
    public string Message { get; set; } = string.Empty;
    /// <summary>Nombres de claves escritas/actualizadas (sin valores secretos).</summary>
    public IReadOnlyList<string> EnvKeysUpdated { get; set; } = Array.Empty<string>();
    public string EnvFilePath { get; set; } = string.Empty;
    public string DotEnvPath { get; set; } = string.Empty;
    public string RestartHint { get; set; } = string.Empty;
    public InstallerStatusResponse Status { get; set; } = new();
}
