using OCAP.Agents.Domain.Entities;

namespace OCAP.Agents.Abstractions.Ports;

// Puerto de persistencia para la gestión de Agentes Inteligentes.
public interface IAgentRepository
{
    // Obtiene un agente por su identificador único.
    Task<Agent?> GetByIdAsync(Guid agentId, CancellationToken cancellationToken = default);

    // Obtiene el agente por defecto asignado para responder tráfico general.
    Task<Agent?> GetDefaultAgentAsync(CancellationToken cancellationToken = default);

    // Guarda o actualiza un agente en la base de datos.
    Task SaveAsync(Agent agent, CancellationToken cancellationToken = default);

    Task<IEnumerable<Agent>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Agent agent, CancellationToken cancellationToken = default);
    Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default);
    Task DeleteAsync(Agent agent, CancellationToken cancellationToken = default);
}
