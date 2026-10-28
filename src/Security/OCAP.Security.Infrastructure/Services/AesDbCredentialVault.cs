using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.Options;

namespace OCAP.Security.Infrastructure.Services;

// Implementación de ICredentialVault con AES-256-GCM: cifrado autenticado con una clave por organización derivada de la
// clave maestra (HKDF). La organización y el nombre del secreto van como datos asociados, así una referencia no se
// puede alterar ni mover a otra organización sin que el descifrado falle. Las referencias anteriores (AES-CBC, formato
// vault:aes256) se siguen leyendo.
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
            var name = secretKey.Trim();
            var encryptedBase64 = EncryptGcm(secretValue, DeriveGcmKey(tenantId), AssociatedData(tenantId, name));
            var secretRef = $"{GcmPrefix}:{tenantId:N}:{name}:{encryptedBase64}";

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
            var scheme = parts.Length >= 5 && parts[0] == "vault" ? parts[1] : null;
            if (scheme is not ("aes256" or "aesgcm"))
            {
                _logger.LogWarning("Formato de referencia de Vault no reconocido o inválido.");
                return Task.FromResult<string?>(null);
            }

            var refTenantStr = parts[2];
            var name = string.Join(':', parts[3..^1]);
            var encryptedBase64 = parts[^1];

            if (!Guid.TryParseExact(refTenantStr, "N", out var refTenantId) || refTenantId != tenantId)
            {
                _logger.LogWarning("Violación de aislamiento multi-tenant: La referencia del secreto no pertenece al TenantId {TenantId}.", tenantId);
                return Task.FromResult<string?>(null);
            }

            var decryptedText = scheme == "aesgcm"
                ? DecryptGcm(encryptedBase64, DeriveGcmKey(tenantId), AssociatedData(tenantId, name))
                : DecryptLegacyCbc(encryptedBase64, DeriveLegacyTenantKey(tenantId));
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

    private const string GcmPrefix = "vault:aesgcm";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private byte[] DeriveGcmKey(Guid tenantId) =>
        HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(_masterKey), 32,
            info: Encoding.UTF8.GetBytes($"ocap-vault-v2:{tenantId:N}"));

    private static byte[] AssociatedData(Guid tenantId, string name) => Encoding.UTF8.GetBytes($"{tenantId:N}:{name}");

    // nonce (12) | tag (16) | texto cifrado
    private static string EncryptGcm(string plainText, byte[] key, byte[] associatedData)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var payload = new byte[NonceSize + TagSize + plainBytes.Length];
        var nonce = payload.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, payload.AsSpan(NonceSize + TagSize), payload.AsSpan(NonceSize, TagSize), associatedData);
        return Convert.ToBase64String(payload);
    }

    private static string DecryptGcm(string cipherTextBase64, byte[] key, byte[] associatedData)
    {
        var payload = Convert.FromBase64String(cipherTextBase64);
        var plainBytes = new byte[payload.Length - NonceSize - TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(payload.AsSpan(0, NonceSize), payload.AsSpan(NonceSize + TagSize), payload.AsSpan(NonceSize, TagSize),
            plainBytes, associatedData);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private byte[] DeriveLegacyTenantKey(Guid tenantId) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"{_masterKey}_{tenantId:N}"));

    private static string DecryptLegacyCbc(string cipherTextBase64, byte[] key)
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
