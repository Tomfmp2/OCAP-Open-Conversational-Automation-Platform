namespace OCAP.Security.Abstractions;

// Contrato para el almacenamiento, descifrado y gestión segura de credenciales sensibles (API keys, bot tokens, OAuth refresh tokens).
public interface ICredentialVault
{
    // Almacena un secreto de forma cifrada para un Tenant y retorna la referencia inmutable al vault.
    Task<string> StoreSecretAsync(Guid tenantId, string secretKey, string secretValue, CancellationToken cancellationToken = default);

    // Recupera y descifra el secreto a partir de la referencia del vault.
    Task<string?> RetrieveSecretAsync(Guid tenantId, string secretReference, CancellationToken cancellationToken = default);

    // Elimina un secreto del vault a partir de su referencia.
    Task<bool> DeleteSecretAsync(Guid tenantId, string secretReference, CancellationToken cancellationToken = default);
}
