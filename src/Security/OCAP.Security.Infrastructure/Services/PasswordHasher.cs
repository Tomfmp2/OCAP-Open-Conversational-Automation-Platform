using System.Security.Cryptography;
using OCAP.Security.Abstractions;

namespace OCAP.Security.Infrastructure.Services;

// Implementación de hashing seguro de contraseñas mediante PBKDF2 (SHA256) con 100,000 iteraciones y salt dinámico.
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public (string Hash, string Salt) HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("La contraseña no puede ser vacía.", nameof(password));

        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        byte[] hashBytes = pbkdf2.GetBytes(HashSize);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
            return false;

        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] expectedHashBytes = Convert.FromBase64String(hash);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
        byte[] actualHashBytes = pbkdf2.GetBytes(HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
    }
}
