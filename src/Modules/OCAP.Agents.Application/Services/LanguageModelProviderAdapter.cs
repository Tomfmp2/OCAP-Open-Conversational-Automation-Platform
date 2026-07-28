using OCAP.Agents.Abstractions.Providers;
using OCAP.Intelligence.Abstractions;

namespace OCAP.Agents.Application.Services;

// Adaptador hexagonal que conecta IAiProvider (Intelligence) con ILanguageModelProvider (Agents Runtime).
public class LanguageModelProviderAdapter : ILanguageModelProvider
{
    private readonly IAiProvider _innerProvider;

    public string ProviderName => _innerProvider.Name;

    public LanguageModelProviderAdapter(IAiProvider innerProvider)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
    }

    public async Task<LanguageModelResponse> GenerateAsync(LanguageModelRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var systemMsg = request.Messages.FirstOrDefault(m => m.Role == MessageRole.System)?.Content ?? string.Empty;
        var userMsg = request.Messages.LastOrDefault(m => m.Role == MessageRole.User)?.Content ?? string.Empty;

        var history = request.Messages
            .Where(m => m.Role != MessageRole.System && m.Content != userMsg)
            .Select(m => $"{m.Role}: {m.Content}")
            .ToList();

        var aiRequest = new AiRequest
        {
            UserMessage = userMsg,
            SystemInstructions = systemMsg,
            ConversationHistory = history,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens
        };

        var response = await _innerProvider.GenerateResponseAsync(aiRequest, cancellationToken);

        return new LanguageModelResponse(
            content: response.GeneratedText,
            providerName: response.ProviderName,
            modelUsed: response.ModelName,
            tokensUsed: response.TokensUsed);
    }
}
