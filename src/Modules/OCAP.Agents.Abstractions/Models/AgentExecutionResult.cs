namespace OCAP.Agents.Abstractions.Models;

public class AgentExecutionResult
{
    public bool Success { get; }
    public string OutputMessage { get; }
    public Guid AgentId { get; }
    public string ProviderUsed { get; }
    public IDictionary<string, object> Metadata { get; }

    public AgentExecutionResult(
        bool success,
        string outputMessage,
        Guid agentId,
        string providerUsed = "Default",
        IDictionary<string, object>? metadata = null)
    {
        Success = success;
        OutputMessage = outputMessage ?? string.Empty;
        AgentId = agentId;
        ProviderUsed = providerUsed;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    public static AgentExecutionResult CreateSuccess(string message, Guid agentId, string providerUsed = "Default", IDictionary<string, object>? metadata = null)
        => new(true, message, agentId, providerUsed, metadata);

    public static AgentExecutionResult CreateFailure(string errorMessage, Guid agentId, string providerUsed = "Default")
        => new(false, errorMessage, agentId, providerUsed);
}
