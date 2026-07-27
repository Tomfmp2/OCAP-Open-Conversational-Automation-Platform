using OCAP.Tools.Abstractions;

namespace OCAP.Agents.Application.Services;

// Registro en memoria para administrar las herramientas ejecutables disponibles en OCAP.
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterTool(ITool tool)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));
        if (string.IsNullOrWhiteSpace(tool.Metadata.Name)) throw new ArgumentException("La herramienta debe poseer un nombre válido.");

        _tools[tool.Metadata.Name] = tool;
    }

    public ITool? GetTool(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        _tools.TryGetValue(name, out var tool);
        return tool;
    }

    public IReadOnlyCollection<ITool> GetAllTools()
    {
        return _tools.Values.ToList().AsReadOnly();
    }
}
