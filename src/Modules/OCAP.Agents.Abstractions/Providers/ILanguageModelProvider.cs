namespace OCAP.Agents.Abstractions.Providers;

// Contrato agnóstico para proveedores de LLM compatibles con OCAP (OpenAI, Gemini, Ollama, Modelos Locales).
public interface ILanguageModelProvider
{
    string ProviderName { get; }
    Task<LanguageModelResponse> GenerateAsync(LanguageModelRequest request, CancellationToken cancellationToken = default);
}

// Selector de proveedor activo según la configuración dinámica del tenant o del instalador.
public interface ILanguageModelProviderSelector
{
    Task<ILanguageModelProvider> GetProviderAsync(Guid tenantId, string? preferredProvider = null, CancellationToken cancellationToken = default);
}
