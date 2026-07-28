using OCAP.Agents.Abstractions.Providers;

namespace OCAP.Agents.Application.Services;

// Implementación por defecto del selector de proveedores de modelos de lenguaje (OpenAI, Gemini, Ollama, Local).
public class DefaultLanguageModelProviderSelector : ILanguageModelProviderSelector
{
    private readonly IEnumerable<ILanguageModelProvider> _providers;

    public DefaultLanguageModelProviderSelector(IEnumerable<ILanguageModelProvider> providers)
    {
        _providers = providers;
    }

    public Task<ILanguageModelProvider> GetProviderAsync(Guid tenantId, string? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        var targetName = preferredProvider ?? "Mock";
        var selected = _providers.FirstOrDefault(p => string.Equals(p.ProviderName, targetName, StringComparison.OrdinalIgnoreCase))
            ?? _providers.FirstOrDefault()
            ?? new FallbackLanguageModelProvider();

        return Task.FromResult(selected);
    }
}

public class FallbackLanguageModelProvider : ILanguageModelProvider
{
    public string ProviderName => "Fallback";

    public Task<LanguageModelResponse> GenerateAsync(LanguageModelRequest request, CancellationToken cancellationToken = default)
    {
        var userMsg = request.Messages.LastOrDefault(m => m.Role == MessageRole.User)?.Content ?? "";
        return Task.FromResult(new LanguageModelResponse(
            $"[OCAP Core Response]: Procesado exitosamente ('{userMsg}').",
            ProviderName,
            "fallback-v1",
            15));
    }
}
