using System.Security.Cryptography;
using System.Text;

namespace OCAP.Channels.WhatsApp.Evolution;

/// <summary>
/// Una instancia de Evolution por organización, con nombre determinista: el webhook sabe de qué organización es
/// un evento sin consultar nada, y vincular dos veces reutiliza la misma instancia.
/// </summary>
public static class EvolutionInstanceIdentity
{
    private const string Prefix = "ocap-";

    /// <summary>Cabecera que OCAP pide a Evolution enviar en cada webhook de la instancia.</summary>
    public const string SignatureHeader = "X-OCAP-Webhook-Signature";

    public static string InstanceNameFor(Guid tenantId) => $"{Prefix}{tenantId:N}";

    public static bool TryParseTenant(string? instanceName, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        return instanceName is not null
               && instanceName.StartsWith(Prefix, StringComparison.Ordinal)
               && Guid.TryParseExact(instanceName[Prefix.Length..], "N", out tenantId)
               && tenantId != Guid.Empty;
    }

    /// <summary>
    /// Firma de la instancia: HMAC-SHA256 del nombre con el secreto global. Cada organización tiene la suya, así que
    /// la de una no sirve para hacerse pasar por otra.
    /// </summary>
    public static string SignatureFor(string instanceName, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(instanceName)));
    }

    public static bool IsValidSignature(string instanceName, string? signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret)) return false;
        var expected = Encoding.ASCII.GetBytes(SignatureFor(instanceName, secret));
        var received = Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(expected, received);
    }
}
