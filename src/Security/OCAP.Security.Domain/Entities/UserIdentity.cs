namespace OCAP.Security.Domain.Entities;

// Entidad central de identidad de usuario con soporte multi-tenant y autenticación segura.
public class UserIdentity
{
    // Identificador único global del usuario.
    public Guid Id { get; private set; }

    // Identificador del tenant al que pertenece el usuario.
    public Guid TenantId { get; private set; }

    // Dirección de correo electrónico única para inicio de sesión.
    public string Email { get; private set; } = string.Empty;

    // Hash de la contraseña generado mediante PBKDF2 con Salt dinámico.
    public string PasswordHash { get; private set; } = string.Empty;

    // Salt único por usuario para prevenir ataques de Rainbow Tables.
    public string Salt { get; private set; } = string.Empty;

    // Nombre completo del usuario.
    public string FullName { get; private set; } = string.Empty;

    // Estado de activación de la cuenta de usuario.
    public bool IsActive { get; private set; } = true;

    // Marca de tiempo UTC de creación de la cuenta.
    public DateTime CreatedAtUtc { get; private set; }

    private UserIdentity() { } // Constructor privado para el ORM.

    // Constructor de dominio con validaciones de invariante.
    public UserIdentity(Guid id, Guid tenantId, string email, string passwordHash, string salt, string fullName)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de usuario no puede ser vacío.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("El ID de tenant no puede ser vacío.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("El email es requerido.", nameof(email));

        Id = id;
        TenantId = tenantId;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Salt = salt;
        FullName = fullName ?? string.Empty;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    // Actualiza la contraseña del usuario restableciendo el Hash y Salt.
    public void UpdatePassword(string newPasswordHash, string newSalt)
    {
        PasswordHash = newPasswordHash;
        Salt = newSalt;
    }

    // Desactiva la cuenta del usuario impidiendo futuros inicios de sesión.
    public void Deactivate() => IsActive = false;
}
