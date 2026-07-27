namespace OCAP.Tools.Abstractions;

// Registro central para descubrir y obtener herramientas disponibles en el sistema.
public interface IToolRegistry
{
    // Registra una nueva herramienta ejecutable en el contenedor.
    void RegisterTool(ITool tool);

    // Obtiene una herramienta por su nombre único identificador.
    ITool? GetTool(string name);

    // Obtiene la colección completa de herramientas disponibles en el sistema.
    IReadOnlyCollection<ITool> GetAllTools();
}
