namespace OCAP.Security.Abstractions;

// Contrato para el hashing seguro de contraseñas mediante PBKDF2 y salado dinámico.
public interface IPasswordHasher
{
    // Genera un hash criptográfico y salt único para una contraseña en texto plano.
    (string Hash, string Salt) HashPassword(string password);

    // Verifica si la contraseña provista coincide con el hash y salt almacenados.
    bool VerifyPassword(string password, string hash, string salt);

    // true si el hash usa parámetros anteriores a los actuales: se re-hashea tras un login correcto.
    bool NeedsRehash(string hash);
}
