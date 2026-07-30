using OCAP.Agents.Domain.ValueObjects;

namespace OCAP.Agents.Domain.Entities;

public enum AgentStatus
{
    Active,
    Inactive,
    Maintenance
}

// Entidad/Aggregate Root que representa un Agente Inteligente dentro de OCAP.
// Controla la identidad, el estado operativo y la configuración del asistente.
public class Agent
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public AgentName Name { get; private set; }
    public string Description { get; private set; }
    public AgentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public AgentConfiguration Configuration { get; private set; }

    private Agent() 
    {
        Name = new AgentName("Default Agent");
        Description = string.Empty;
        Configuration = new AgentConfiguration(string.Empty);
    } // Constructor ORM

    public Agent(Guid id, AgentName name, string description, AgentConfiguration configuration, Guid tenantId = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("El ID del agente no puede ser vacío.", nameof(id));

        Id = id;
        TenantId = tenantId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Status = AgentStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = AgentStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = AgentStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMaintenance()
    {
        Status = AgentStatus.Maintenance;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateConfiguration(AgentConfiguration newConfiguration)
    {
        Configuration = newConfiguration ?? throw new ArgumentNullException(nameof(newConfiguration));
        UpdatedAt = DateTime.UtcNow;
    }
}
