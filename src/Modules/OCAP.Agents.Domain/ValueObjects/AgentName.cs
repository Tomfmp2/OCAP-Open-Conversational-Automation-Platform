namespace OCAP.Agents.Domain.ValueObjects;

// Objeto de valor que representa y valida el nombre de un agente.
public class AgentName
{
    public string Value { get; }

    public AgentName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El nombre del agente no puede estar vacío.", nameof(value));
        }

        if (value.Length > 100)
        {
            throw new ArgumentException("El nombre del agente no puede exceder los 100 caracteres.", nameof(value));
        }

        Value = value.Trim();
    }

    public override bool Equals(object? obj) => obj is AgentName other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(AgentName agentName) => agentName.Value;
}
