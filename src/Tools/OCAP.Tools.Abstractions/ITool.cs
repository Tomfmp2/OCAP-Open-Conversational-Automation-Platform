namespace OCAP.Tools.Abstractions;

// Contrato fundamental que representa una herramienta externa invocable por un agente.
// Desacopla las capacidades de automatización (Google, email, etc.) de la lógica conversacional.
public interface ITool
{
    // Obtiene los metadatos descriptivos de la herramienta.
    ToolMetadata Metadata { get; }

    // Ejecuta la capacidad de la herramienta con argumentos arbitrarios provistos por el agente.
    Task<ToolExecutionResult> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken = default);
}
