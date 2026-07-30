namespace OCAP.Security.Abstractions.Options;

/// <summary>
/// Opciones de emisión y validación de JWT para OCAP.
/// Los secretos deben provenir de configuración o variables de entorno; nunca de literales en código.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumSecretLength = 32;

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "OCAP";
    public string Audience { get; set; } = "OCAP.Clients";
    public int AccessTokenExpiryMinutes { get; set; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey (o JWT_SECRET_KEY) es obligatorio. Configure el secreto vía variables de entorno o secrets del host.");
        }

        if (SecretKey.Length < MinimumSecretLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SecretKey debe tener al menos {MinimumSecretLength} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Audience es obligatorio.");
        }

        if (AccessTokenExpiryMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:AccessTokenExpiryMinutes debe ser mayor que cero.");
        }
    }
}
