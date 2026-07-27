namespace OCAP.Tools.Abstractions;

public enum ToolStatus
{
    Active,
    Disabled,
    Deprecated
}

// Representa la definición y capacidades de una herramienta ejecutable por los agentes.
public class ToolDefinition
{
    // Identificador único de la definición de la herramienta.
    public string Id { get; set; } = string.Empty;

    // Nombre amigable identificador de la herramienta.
    public string Name { get; set; } = string.Empty;

    // Descripción funcional detallada del propósito de la herramienta.
    public string Description { get; set; } = string.Empty;

    // Versión semántica del adaptador de la herramienta.
    public string Version { get; set; } = "1.0.0";

    // Permisos requeridos para que un agente pueda ejecutar la herramienta (ej. "Calendar.Create", "Gmail.Send").
    public List<string> RequiredPermissions { get; set; } = new();

    // Esquema descriptivo del objeto de entrada esperado.
    public string InputSchema { get; set; } = "{}";

    // Esquema descriptivo del objeto de salida producido.
    public string OutputSchema { get; set; } = "{}";

    // Estado operativo actual de la herramienta.
    public ToolStatus Status { get; set; } = ToolStatus.Active;
}
