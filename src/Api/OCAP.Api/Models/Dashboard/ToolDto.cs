namespace OCAP.Api.Models.Dashboard;

// DTO descriptivo para el catálogo de Herramientas en el Dashboard.
public class ToolDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Status { get; set; } = "Active";
    public List<string> RequiredPermissions { get; set; } = new();
}
