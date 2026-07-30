using Microsoft.Extensions.Configuration;
using OCAP.Security.Abstractions.Options;

namespace OCAP.Security.Infrastructure.Configuration;

/// <summary>
/// Resuelve secretos de seguridad desde configuración tipada y variables de entorno equivalentes.
/// </summary>
public static class SecurityConfigurationBinder
{
    public static JwtOptions BindJwtOptions(IConfiguration configuration)
    {
        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            options.SecretKey = configuration["JWT_SECRET_KEY"] ?? string.Empty;
        }

        options.Validate();
        return options;
    }

    public static VaultOptions BindVaultOptions(IConfiguration configuration)
    {
        var options = configuration.GetSection(VaultOptions.SectionName).Get<VaultOptions>() ?? new VaultOptions();

        if (string.IsNullOrWhiteSpace(options.MasterKey))
        {
            options.MasterKey = configuration["VAULT_MASTER_KEY"] ?? string.Empty;
        }

        options.Validate();
        return options;
    }
}
