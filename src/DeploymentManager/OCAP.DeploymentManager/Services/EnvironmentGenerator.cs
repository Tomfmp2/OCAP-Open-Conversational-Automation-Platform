using OCAP.DeploymentManager.Models;

namespace OCAP.DeploymentManager.Services;

// Servicio encargado de la generación segura de archivos .env para el despliegue en Docker.
public class EnvironmentGenerator
{
    public string GenerateEnvironmentFileContent(DeploymentConfiguration config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Archivo de configuración generado por OCAP Deployment Manager");
        sb.AppendLine($"# Modo de instalación: {config.Mode}");
        sb.AppendLine($"# Fecha de generación UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("# Base de Datos PostgreSQL");
        sb.AppendLine($"POSTGRES_HOST={config.PostgresHost}");
        sb.AppendLine($"POSTGRES_PORT={config.PostgresPort}");
        sb.AppendLine($"POSTGRES_DB={config.PostgresDbName}");
        sb.AppendLine($"POSTGRES_USER={config.PostgresUsername}");
        sb.AppendLine($"POSTGRES_PASSWORD={config.PostgresPassword}");
        sb.AppendLine();

        sb.AppendLine("# Canal WhatsApp Evolution API");
        sb.AppendLine($"EVOLUTION_API_URL={config.EvolutionApiUrl}");
        sb.AppendLine($"EVOLUTION_API_KEY={config.EvolutionApiKey}");
        sb.AppendLine();

        sb.AppendLine("# Google Workspace OAuth");
        sb.AppendLine($"GOOGLE_CLIENT_ID={config.GoogleClientId}");
        sb.AppendLine($"GOOGLE_CLIENT_SECRET={config.GoogleClientSecret}");
        sb.AppendLine($"GOOGLE_REDIRECT_URI={config.GoogleRedirectUri}");
        sb.AppendLine();

        sb.AppendLine("# Seguridad & JWT");
        sb.AppendLine($"JWT_SECRET_KEY={config.JwtSecretKey}");

        return sb.ToString();
    }
}
