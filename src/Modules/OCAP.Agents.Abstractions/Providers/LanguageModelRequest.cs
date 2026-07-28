namespace OCAP.Agents.Abstractions.Providers;

public class LanguageModelRequest
{
    public string ModelName { get; }
    public IReadOnlyList<PromptMessage> Messages { get; }
    public double Temperature { get; }
    public int MaxTokens { get; }
    public IDictionary<string, object> Parameters { get; }

    public LanguageModelRequest(
        IEnumerable<PromptMessage> messages,
        string modelName = "default",
        double temperature = 0.7,
        int maxTokens = 2048,
        IDictionary<string, object>? parameters = null)
    {
        Messages = messages?.ToList() ?? new List<PromptMessage>();
        ModelName = modelName;
        Temperature = temperature;
        MaxTokens = maxTokens;
        Parameters = parameters ?? new Dictionary<string, object>();
    }
}
