namespace OCAP.Security.Abstractions;

// Contrato para generación y validación de TOTP según RFC 6238 / RFC 4226 (CAP-17).
public interface ITotpService
{
    string GenerateSecretKey();
    string GenerateQrCodeUri(string userEmail, string secret, string issuer = "OCAP");
    bool ValidateCode(string secret, string code, int timeStepSeconds = 30);
}
