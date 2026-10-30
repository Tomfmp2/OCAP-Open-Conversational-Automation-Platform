using System.Security.Cryptography;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

// Hashing de contraseñas con PBKDF2-SHA256 y salt dinámico. Los hashes nuevos llevan el formato
// "pbkdf2-sha256$<iteraciones>$<hash>"; los anteriores (solo base64) se verificaron con 100.000 iteraciones y se
// re-hashean al iniciar sesión (NeedsRehash).
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const string Scheme = "pbkdf2-sha256";
    // Recomendación de OWASP (2023) para PBKDF2-HMAC-SHA256.
    public const int Iterations = 600_000;
    private const int LegacyIterations = 100_000;

    public (string Hash, string Salt) HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("La contraseña no puede ser vacía.", nameof(password));

        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSize);

        return ($"{Scheme}${Iterations}${Convert.ToBase64String(hashBytes)}", Convert.ToBase64String(saltBytes));
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
            return false;
        if (!TryParse(hash, out var iterations, out var expectedHashBytes)) return false;

        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, iterations, HashAlgorithmName.SHA256, HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
    }

    public bool NeedsRehash(string hash) => !TryParse(hash, out var iterations, out _) || iterations < Iterations;

    private static bool TryParse(string hash, out int iterations, out byte[] hashBytes)
    {
        iterations = LegacyIterations;
        hashBytes = [];
        try
        {
            var parts = hash.Split('$');
            if (parts.Length == 3 && parts[0] == Scheme && int.TryParse(parts[1], out iterations) && iterations > 0)
            {
                hashBytes = Convert.FromBase64String(parts[2]);
                return true;
            }
            if (parts.Length != 1) return false;

            hashBytes = Convert.FromBase64String(hash);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
