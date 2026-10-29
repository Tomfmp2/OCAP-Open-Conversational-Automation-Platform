using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OCAP.Security.Abstractions.Options;

namespace OCAP.Api.Services;

/// <summary>
/// Parámetro <c>state</c> del flujo OAuth de Google, firmado por la API: ata la respuesta de Google a quien inició la
/// conexión (organización y usuario) y caduca a los 10 minutos. Sin guardar nada en el navegador ni en la base.
/// </summary>
public sealed class GoogleOAuthState
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly byte[] _key;

    public GoogleOAuthState(IOptions<JwtOptions> jwt) =>
        // Clave derivada del secreto de la API, distinta de la que firma las sesiones.
        _key = SHA256.HashData(Encoding.UTF8.GetBytes("ocap-google-oauth-state|" + jwt.Value.SecretKey));

    public string Create(Guid tenantId, Guid userId)
    {
        var payload = $"{tenantId:N}.{userId:N}.{DateTimeOffset.UtcNow.Add(Lifetime).ToUnixTimeSeconds()}.{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8))}";
        return $"{payload}.{Sign(payload)}";
    }

    public bool IsValid(string? state, Guid tenantId, Guid userId)
    {
        var parts = state?.Split('.');
        if (parts is not { Length: 5 }) return false;
        var payload = string.Join('.', parts[..4]);
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(payload)), Encoding.ASCII.GetBytes(parts[4]))
               && parts[0] == tenantId.ToString("N")
               && parts[1] == userId.ToString("N")
               && long.TryParse(parts[2], out var expires)
               && DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= expires;
    }

    private string Sign(string payload) => Convert.ToHexStringLower(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload)));
}
