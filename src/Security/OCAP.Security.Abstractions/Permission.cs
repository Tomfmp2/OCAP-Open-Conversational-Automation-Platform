namespace OCAP.Security.Abstractions;

// Representa un permiso del sistema o de herramientas en OCAP (ej. "Calendar.Create", "Gmail.Send").
public class Permission
{
    // Nombre único identificador del permiso.
    public string Name { get; }

    // Categoria funcional del permiso (ej. "Google.Calendar", "Google.Gmail").
    public string Category { get; }

    // Descripción del alcance y propósito del permiso.
    public string Description { get; }

    public Permission(string name, string category, string description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre del permiso no puede estar vacío.", nameof(name));
        
        Name = name.Trim();
        Category = category?.Trim() ?? "General";
        Description = description?.Trim() ?? string.Empty;
    }

    public override bool Equals(object? obj) => obj is Permission other && Name == other.Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
}
