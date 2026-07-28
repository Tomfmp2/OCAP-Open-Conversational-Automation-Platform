namespace OCAP.Agents.Abstractions.Providers;

public class LanguageModelResponse
{
    public string Content { get; }
    public string ProviderName { get; }
    public string ModelUsed { get; }
    public int TokensUsed { get; }

    public LanguageModelResponse(string content, string providerName, string modelUsed = "default", int tokensUsed = 0)
    {
        Content = content ?? string.Empty;
        ProviderName = providerName ?? "Unknown";
        ModelUsed = modelUsed;
        TokensUsed = tokensUsed;
    }
}
