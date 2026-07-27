using FluentAssertions;
using OCAP.Tools.Abstractions;
using OCAP.Tools.Google;
using OCAP.Providers.Google.Calendar;

namespace OCAP.Tools.Tests;

public class ToolRegistryTests
{
    [Fact]
    public void RegisterTool_WhenValid_StoresToolInRegistry()
    {
        // Arrange
        var registry = new ToolRegistryImpl();
        var tool = new CreateCalendarEventTool(new InMemoryCalendarProvider());

        // Act
        registry.RegisterTool(tool);

        // Assert
        var resolved = registry.GetTool("CreateCalendarEventTool");
        resolved.Should().NotBeNull();
        resolved!.Definition.Name.Should().Be("CreateCalendarEventTool");
    }

    [Fact]
    public void GetTool_WhenNonExistent_ReturnsNull()
    {
        // Arrange
        var registry = new ToolRegistryImpl();

        // Act
        var resolved = registry.GetTool("NonExistentTool");

        // Assert
        resolved.Should().BeNull();
    }
}

// Implementación simple de IToolRegistry para la prueba.
internal class ToolRegistryImpl : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterTool(ITool tool) => _tools[tool.Definition.Name] = tool;
    public ITool? GetTool(string name) => _tools.TryGetValue(name, out var t) ? t : null;
    public IReadOnlyCollection<ITool> GetAllTools() => _tools.Values.ToList().AsReadOnly();
}
