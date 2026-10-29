using System.Security.Cryptography;
using System.Text;

namespace OCAP.Api.Services;

/// <summary>
/// Secretos del webhook de WhatsApp Cloud guardados en cada conexión (metadata): la referencia al App Secret cifrado
/// en el vault y el hash del token de verificación que se pega en Meta.
/// </summary>
public static class WhatsAppCloudSecrets
{
    public const string AppSecretRefKey = "AppSecretRef";
    public const string VerifyTokenHashKey = "VerifyTokenHash";

    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static bool Matches(string token, string? storedHash) =>
        storedHash is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(token)), Encoding.ASCII.GetBytes(storedHash));
}
