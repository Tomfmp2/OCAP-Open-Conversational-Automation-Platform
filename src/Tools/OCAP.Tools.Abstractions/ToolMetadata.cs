namespace OCAP.Tools.Abstractions;

// Información descriptiva y estado de una herramienta (Tool) ejecutable por los agentes de OCAP.
public class ToolMetadata
{
    // Nombre único identificador de la herramienta (ej. "GoogleCalendarTool", "GmailTool").
    public string Name { get; set; } = string.Empty;

    // Descripción del propósito y capacidades de la herramienta.
    public string Description { get; set; } = string.Empty;

    // Categoría funcional de la herramienta (ej. "Calendar", "Email", "Automation").
    public string Category { get; set; } = "General";

    // Versión del adaptador de la herramienta.
    public string Version { get; set; } = "1.0.0";

    // Indica si la herramienta está habilitada para ejecución.
    public bool IsEnabled { get; set; } = true;
}
