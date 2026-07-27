namespace OCAP.DeploymentManager.Models;

public enum InstallationMode
{
    LocalDevelopment = 1,
    PersonalServer = 2,
    EnterpriseServer = 3
}

// Configuración completa para el despliegue autohospedado de OCAP.
public class DeploymentConfiguration
{
    public InstallationMode Mode { get; set; } = InstallationMode.LocalDevelopment;

    // Base de datos PostgreSQL
    public string PostgresHost { get; set; } = "localhost";
    public int PostgresPort { get; set; } = 5432;
    public string PostgresDbName { get; set; } = "ocap_db";
    public string PostgresUsername { get; set; } = "ocap_user";
    public string PostgresPassword { get; set; } = "OcapSecurePass2026!";

    // Canales
    public bool EnableWhatsApp { get; set; } = true;
    public string EvolutionApiUrl { get; set; } = "http://localhost:8080";
    public string EvolutionApiKey { get; set; } = "EvolutionSecretApiKey";

    // Google Workspace OAuth
    public bool EnableGoogleWorkspace { get; set; } = true;
    public string GoogleClientId { get; set; } = "your-google-client-id.apps.googleusercontent.com";
    public string GoogleClientSecret { get; set; } = "your-google-client-secret";
    public string GoogleRedirectUri { get; set; } = "http://localhost:5000/api/auth/google/callback";

    // Seguridad & Claves
    public string JwtSecretKey { get; set; } = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
}
