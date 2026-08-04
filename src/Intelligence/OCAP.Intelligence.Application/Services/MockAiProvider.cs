using System.Runtime.CompilerServices;
using System.Text.Json;
using OCAP.Agents.Domain.Entities;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Domain;

namespace OCAP.Intelligence.Application.Services;

/// <summary>
/// Proveedor local de desarrollo: responde sin llamar APIs externas.
/// Activo cuando AiProviders:EnableMock=true o UseInMemory=true.
/// </summary>
public sealed class MockAiProvider : IAiProvider
{
    public string Name => "Mock";

    public Task<AiResponse> GenerateResponseAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var user = string.IsNullOrWhiteSpace(request.UserMessage) ? "(vacío)" : request.UserMessage.Trim();
        var text =
            $"[Mock AI] Recibí tu mensaje: «{user}». " +
            "Este es el proveedor de desarrollo de OCAP. " +
            "Configura AiProviders:OpenAI:ApiKey (o Gemini/Claude/Ollama) en .env / appsettings para respuestas reales.";

        return Task.FromResult(new AiResponse
        {
            GeneratedText = text,
            TokensUsed = Math.Max(8, user.Length / 4),
            ModelName = "mock-ocap-1",
            ProviderName = Name,
            Metadata = new Dictionary<string, object>
            {
                ["LatencyMs"] = 1,
                ["Mode"] = "offline-dev"
            }
        });
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        AiRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var full = (await GenerateResponseAsync(request, cancellationToken)).GeneratedText;
        foreach (var word in full.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return word + " ";
            await Task.Delay(5, cancellationToken);
        }
    }

    public Task<Intent> AnalyzeIntentAsync(string message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Intent(Intent.GetInformation, 0.55));
    }

    public Task<T?> ExtractStructuredDataAsync<T>(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(text));
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    public AiModelInformation GetModelInformation() => new()
    {
        Provider = Name,
        Model = "mock-ocap-1",
        ContextSize = 4096,
        Capabilities = new List<string> { "chat", "dev", "offline" }
    };

    public Task<ProviderHealth> HealthAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProviderHealth
        {
            ProviderName = Name,
            IsHealthy = true,
            LatencyMs = 0,
            ModelList = new List<string> { "mock-ocap-1" },
            Version = "1.0.0",
            StatusMessage = "Mock provider ready (no external API)"
        });

    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(new[] { "mock-ocap-1" });
}
