using System.Security.Cryptography;
using System.Text;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

// Implementación de firma HMAC SHA-256 para cargas de webhook.
public class HmacSha256WebhookSigner : IWebhookSigner
{
    public string SignPayload(string payloadJson, string secret)
    {
        if (string.IsNullOrEmpty(payloadJson)) payloadJson = string.Empty;
        if (string.IsNullOrEmpty(secret)) secret = string.Empty;

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        var hexHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"sha256={hexHash}";
    }

    public bool VerifySignature(string payloadJson, string secret, string expectedSignature)
    {
        if (string.IsNullOrWhiteSpace(expectedSignature)) return false;

        var computed = SignPayload(payloadJson, secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(expectedSignature)
        );
    }
}
