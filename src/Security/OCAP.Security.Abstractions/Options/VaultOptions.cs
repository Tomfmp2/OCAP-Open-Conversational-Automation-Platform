namespace OCAP.Security.Abstractions.Options;

/// <summary>
/// Opciones del vault de credenciales (derivación de clave AES por tenant).
/// </summary>
public sealed class VaultOptions
{
    public const string SectionName = "Security:Vault";
    public const int MinimumMasterKeyLength = 32;

    public string MasterKey { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MasterKey))
        {
            throw new InvalidOperationException(
                "Security:Vault:MasterKey (o VAULT_MASTER_KEY) es obligatorio. Configure la clave maestra vía variables de entorno o secrets del host.");
        }

        if (MasterKey.Length < MinimumMasterKeyLength)
        {
            throw new InvalidOperationException(
                $"Security:Vault:MasterKey debe tener al menos {MinimumMasterKeyLength} caracteres.");
        }
    }
}
