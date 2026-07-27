namespace OCAP.Core.Entities;

// Estado posible de la cuenta de usuario del sistema.
public enum UserStatus
{
    Active,
    Blocked,
    Inactive
}

// Entidad fundamental de Usuario dentro del Dominio puramente DDD.
public class User
{
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public UserStatus Status { get; private set; }

    // Constructor privado para hidratación mediante Entity Framework Core.
    private User()
    {
        DisplayName = string.Empty;
    }

    public User(Guid id, string displayName)
    {
        if (id == Guid.Empty) throw new ArgumentException("User identifier cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name cannot be empty.", nameof(displayName));

        Id = id;
        DisplayName = displayName;
        CreatedAt = DateTime.UtcNow;
        Status = UserStatus.Active;
    }

    public void Block()
    {
        Status = UserStatus.Blocked;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unblock()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetInactive()
    {
        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }
}
