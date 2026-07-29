namespace OCAP.Security.Abstractions;

// Firma de payloads HTTP mediante HMAC SHA-256 para validación de origen en webhooks.
public interface IWebhookSigner
{
    string SignPayload(string payloadJson, string secret);
    bool VerifySignature(string payloadJson, string secret, string expectedSignature);
}
