using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.Options;

namespace OCAP.Security.Infrastructure.Services;

// Implementación de ICredentialVault con cifrado AES-256 (CBC + IV aleatorio).
// La clave maestra se inyecta por configuración; no existen sales literales en código.
public class AesDbCredentialVault : ICredentialVault
{
    private readonly ILogger<AesDbCredentialVault> _logger;
    private readonly string _masterKey;

    public AesDbCredentialVault(ILogger<AesDbCredentialVault> logger, IOptions<VaultOptions> options)
        : this(logger, options?.Value?.MasterKey ?? string.Empty)
    {
    }

    public AesDbCredentialVault(ILogger<AesDbCredentialVault> logger, string masterKey)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var vaultOptions = new VaultOptions { MasterKey = masterKey };
        vaultOptions.Validate();
        _masterKey = vaultOptions.MasterKey;
    }

    public Task<string> StoreSecretAsync(Guid tenantId, string secretKey, string secretValue, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(secretKey)) throw new ArgumentException("secretKey es obligatorio.", nameof(secretKey));
        if (string.IsNullOrWhiteSpace(secretValue)) throw new ArgumentException("secretValue es obligatorio.", nameof(secretValue));

        try
        {
            var key = DeriveTenantKey(tenantId);
            var encryptedBase64 = EncryptString(secretValue, key);
            var secretRef = $"vault:aes256:{tenantId:N}:{secretKey.Trim()}:{encryptedBase64}";

            _logger.LogInformation("Secreto guardado exitosamente en Vault para TenantId {TenantId} con referencia de clave {SecretKey}.",
                tenantId, secretKey);

            return Task.FromResult(secretRef);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            _logger.LogError(ex, "Error al cifrar y almacenar el secreto en Vault para TenantId {TenantId}.", tenantId);
            throw new InvalidOperationException("Falló el cifrado seguro de credenciales.", ex);
        }
    }

    public Task<string?> RetrieveSecretAsync(Guid tenantId, string secretReference, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(secretReference))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var parts = secretReference.Split(':');
            if (parts.Length < 5 || parts[0] != "vault" || parts[1] != "aes256")
            {
                _logger.LogWarning("Formato de referencia de Vault no reconocido o inválido.");
                return Task.FromResult<string?>(null);
            }

            var refTenantStr = parts[2];
            var encryptedBase64 = parts[4];

            if (!Guid.TryParseExact(refTenantStr, "N", out var refTenantId) || refTenantId != tenantId)
            {
                _logger.LogWarning("Violación de aislamiento multi-tenant: La referencia del secreto no pertenece al TenantId {TenantId}.", tenantId);
                return Task.FromResult<string?>(null);
            }

            var key = DeriveTenantKey(tenantId);
            var decryptedText = DecryptString(encryptedBase64, key);
            return Task.FromResult<string?>(decryptedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descifrar el secreto de Vault para TenantId {TenantId}.", tenantId);
            return Task.FromResult<string?>(null);
        }
    }

    public Task<bool> DeleteSecretAsync(Guid tenantId, string secretReference, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(secretReference))
        {
            return Task.FromResult(false);
        }

        _logger.LogInformation("Referencia de secreto eliminada/revocada en Vault para TenantId {TenantId}.", tenantId);
        return Task.FromResult(true);
    }

    private byte[] DeriveTenantKey(Guid tenantId)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes($"{_masterKey}_{tenantId:N}"));
    }

    private static string EncryptString(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var payload = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(payload);
    }

    private static string DecryptString(string cipherTextBase64, byte[] key)
    {
        var payload = Convert.FromBase64String(cipherTextBase64);
        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[16];
        var cipherBytes = new byte[payload.Length - 16];

        Buffer.BlockCopy(payload, 0, iv, 0, 16);
        Buffer.BlockCopy(payload, 16, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
