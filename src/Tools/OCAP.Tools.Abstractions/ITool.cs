namespace OCAP.Tools.Abstractions;

// Contrato fundamental que representa una herramienta ejecutable por un agente.
public interface ITool
{
    // Definición y capacidades asociadas a la herramienta.
    ToolDefinition Definition { get; }

    // Ejecuta la herramienta de forma asíncrona recibiendo su contexto de ejecución.
    Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default);
}
