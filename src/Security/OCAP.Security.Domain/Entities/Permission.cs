namespace OCAP.Security.Domain.Entities;

// Entidad que catálogo los permisos granulares del sistema.
public class Permission
{
    // Identificador único del permiso.
    public Guid Id { get; private set; }

    // Código estándar del permiso (ej. Conversation.Read, Agent.Write).
    public string Code { get; private set; } = string.Empty;

    // Nombre amigable para mostrar en la interfaz.
    public string Name { get; private set; } = string.Empty;

    // Categoria funcional a la que pertenece el permiso (ej. Conversations, Agents, AI).
    public string Category { get; private set; } = string.Empty;

    // Descripción del alcance de este permiso.
    public string Description { get; private set; } = string.Empty;

    private Permission() { } // Constructor ORM.

    // Inicializa un objeto de permiso granular.
    public Permission(Guid id, string code, string name, string category, string description)
    {
        Id = id;
        Code = code.Trim();
        Name = name.Trim();
        Category = category.Trim();
        Description = description ?? string.Empty;
    }
}
