namespace OCAP.Security.Abstractions;

// Asocia una herramienta específica con los permisos de seguridad necesarios para ejecutarla.
public class ToolPermission
{
    // Nombre identificador de la herramienta.
    public string ToolName { get; }

    // Nombre identificador del permiso requerido.
    public string PermissionName { get; }

    public ToolPermission(string toolName, string permissionName)
    {
        ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
        PermissionName = permissionName ?? throw new ArgumentNullException(nameof(permissionName));
    }
}
