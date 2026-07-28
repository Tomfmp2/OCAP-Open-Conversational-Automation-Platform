namespace OCAP.Agents.Abstractions.Contracts;

// Contrato que define el entorno de ejecución para agentes en OCAP.
public interface IAgentRuntime
{
    // Ejecuta el ciclo de razonamiento del agente sobre un contexto conversacional.
    Task<string> ExecuteAgentAsync(IAgentContext agentContext, CancellationToken cancellationToken = default);
}
