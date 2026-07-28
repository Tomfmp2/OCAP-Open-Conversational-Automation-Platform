namespace OCAP.Agents.Abstractions.Contracts;

// Contrato de evaluación y resolución para determinar qué agente (Enterprise Assistant Agent o Agente Especializado)
// debe procesar una interacción o solicitud conversacional dentro del ecosistema OCAP.
public interface IAgentResolver
{
    // Evalúa el mensaje conversacional y el contexto del usuario para seleccionar el agente adecuado.
    Task<Guid> ResolveAgentIdAsync(Guid tenantId, Guid userId, string messageContent, CancellationToken cancellationToken = default);
}
