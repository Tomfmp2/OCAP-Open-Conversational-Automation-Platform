using OCAP.DeploymentManager.Models;

namespace OCAP.DeploymentManager.Services;

// Servicio de validación de integridad para parámetros de despliegue antes de la ejecución.
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

        if (config.JwtSecretKey.Length < 16) errors.Add("La clave secreta JWT debe tener al menos 16 caracteres.");

        return (errors.Count == 0, errors);
    }
}
