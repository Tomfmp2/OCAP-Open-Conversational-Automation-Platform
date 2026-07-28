namespace OCAP.Agents.Abstractions.Contracts;

// Contrato para el contexto inmutable o dinámico de ejecución de un agente.
public interface IAgentContext
{
    Guid AgentId { get; }
    Guid TenantId { get; }
    Guid UserId { get; }
    string UserMessage { get; }
    IDictionary<string, object> EnvironmentVariables { get; }
}
