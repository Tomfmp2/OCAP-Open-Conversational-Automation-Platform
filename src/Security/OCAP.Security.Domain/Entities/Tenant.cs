namespace OCAP.Security.Domain.Entities;

// Entidad agregada que representa una organización en la arquitectura Multi-Tenant de OCAP.
public class Tenant
{
    // Identificador único del tenant u organización.
    public Guid Id { get; private set; }

    // Nombre comercial u organizacional del tenant.
    public string Name { get; private set; } = string.Empty;

    // Identificador alfanumérico único para URLs y aislamiento.
    public string Slug { get; private set; } = string.Empty;

    // Estado operativo del tenant en la plataforma.
    public bool IsActive { get; private set; } = true;

    // Configuración serializada en JSON con reglas específicas del tenant.
    public string SettingsJson { get; private set; } = "{}";

    // Marca de tiempo UTC de creación de la organización.
    public DateTime CreatedAtUtc { get; private set; }

    private Tenant() { } // Constructor privado para el ORM.

    // Constructor de dominio para inicializar un nuevo tenant.
    public Tenant(Guid id, string name, string slug)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID de tenant no puede ser vacío.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre del tenant es requerido.", nameof(name));

        Id = id;
        Name = name.Trim();
        Slug = (slug ?? name).Trim().ToLowerInvariant().Replace(" ", "-");
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    // Actualiza la configuración tipada del tenant.
    public void UpdateSettings(string jsonSettings)
    {
        SettingsJson = jsonSettings ?? "{}";
    }

    // Desactiva la organización impidiendo el acceso a todos sus miembros.
    public void Deactivate() => IsActive = false;
}
