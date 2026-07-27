namespace OCAP.Tools.Abstractions;

// Registro central para administrar y descubrir herramientas ejecutables.
public interface IToolRegistry
{
    // Registra una nueva herramienta en el sistema.
    void RegisterTool(ITool tool);

    // Obtiene una herramienta por su nombre identificador.
    ITool? GetTool(string name);

    // Obtiene todas las herramientas registradas en el sistema.
    IReadOnlyCollection<ITool> GetAllTools();
}
