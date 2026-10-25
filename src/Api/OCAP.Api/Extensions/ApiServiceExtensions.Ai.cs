using OCAP.Infrastructure.Services;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Application.Services;
using OCAP.Prompts;
using OCAP.Providers.Gemini;
using OCAP.Providers.Claude;
using OCAP.Providers.Ollama;
using OCAP.Providers.OpenAI;

namespace OCAP.Api.Extensions;

public static partial class ApiServiceExtensions
{
    /// <summary>Proveedores de IA con resiliencia, modo demo, selección por organización y razonamiento.</summary>
    private static void AddAiServices(IServiceCollection services, IConfiguration configuration)
    {
        // Registrar clientes HTTP y proveedores de IA Generativa con Resilience (Polly).
        // LLMs necesitan más de los 10s/30s por defecto del StandardResilienceHandler.
        static void ConfigureLlmResilience(Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions o)
        {
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
            o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(130);
            o.Retry.MaxRetryAttempts = 1;
        }

        services.AddHttpClient<OpenAiProvider>().AddStandardResilienceHandler(ConfigureLlmResilience);
        services.AddHttpClient<GeminiAiProvider>().AddStandardResilienceHandler(ConfigureLlmResilience);
        services.AddHttpClient<OllamaAiProvider>().AddStandardResilienceHandler(ConfigureLlmResilience);
        services.AddHttpClient("Claude").AddStandardResilienceHandler(ConfigureLlmResilience);

        // Clientes con nombre para IAiProvider (el singleton no use un HttpClient “desnudo”).
        services.AddHttpClient("OpenAI").AddStandardResilienceHandler(ConfigureLlmResilience);
        services.AddHttpClient("Gemini").AddStandardResilienceHandler(ConfigureLlmResilience);
        services.AddHttpClient("Ollama").AddStandardResilienceHandler(ConfigureLlmResilience);

        var openAiSettings = new AiProviderSettings
        {
            ApiKey = configuration["AiProviders:OpenAI:ApiKey"] ?? string.Empty,
            BaseUrl = configuration["AiProviders:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1",
            ModelName = configuration["AiProviders:OpenAI:ModelName"] ?? "gpt-4o"
        };

        var geminiSettings = new AiProviderSettings
        {
            ApiKey = configuration["AiProviders:Gemini:ApiKey"] ?? string.Empty,
            ModelName = configuration["AiProviders:Gemini:ModelName"] ?? "gemini-3.5-flash"
        };

        var ollamaSettings = new AiProviderSettings
        {
            BaseUrl = configuration["AiProviders:Ollama:BaseUrl"] ?? "http://localhost:11434",
            ModelName = configuration["AiProviders:Ollama:ModelName"] ?? "llama3"
        };

        var claudeSettings = new AiProviderSettings
        {
            ApiKey = configuration["AiProviders:Claude:ApiKey"] ?? string.Empty,
            BaseUrl = configuration["AiProviders:Claude:BaseUrl"] ?? "https://api.anthropic.com/v1",
            ModelName = configuration["AiProviders:Claude:ModelName"] ?? "claude-sonnet-5-5"
        };

        services.AddSingleton<IAiProvider>(sp => new OpenAiProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenAI"), openAiSettings));
        services.AddSingleton<IAiProvider>(sp => new GeminiAiProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("Gemini"), geminiSettings));
        services.AddSingleton<IAiProvider>(sp => new OllamaAiProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("Ollama"), ollamaSettings));
        services.AddSingleton<IAiProvider>(sp => new ClaudeAiProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("Claude"),
            claudeSettings));

        // Sin ninguna key de IA en el servidor, modo demo: el asistente responde con Mock en vez de fallar.
        // AiProviders:DemoMode=false lo desactiva (p. ej. si solo se usa Ollama, que no necesita key).
        var hasStaticKey = new[] { openAiSettings.ApiKey, geminiSettings.ApiKey, claudeSettings.ApiKey }
            .Any(key => !string.IsNullOrWhiteSpace(key));
        var demoMode = !hasStaticKey && configuration.GetValue("AiProviders:DemoMode", true);
        services.AddSingleton(new AiDemoMode(demoMode));
        if (demoMode || configuration.GetValue("AiProviders:EnableMock", false))
        {
            services.AddSingleton<IAiProvider, OCAP.Intelligence.Application.Services.MockAiProvider>();
        }

        // Registro de Proveedores de IA y Servicio de Configuración por Tenant
        services.AddSingleton<IAiProviderRegistry, AiProviderRegistry>();
        services.AddScoped<IAiProviderConfigurationService, AiProviderConfigurationService>();

        // Orquestador inteligente de proveedores
        services.AddSingleton<IAiProviderSelector, AiProviderSelector>();

        // Prompts y razonamiento
        services.AddSingleton<IPromptBuilder, SystemPromptBuilder>();
        services.AddScoped<IAgentReasoningService, AgentReasoningService>();
        services.AddScoped<IAiUsageTracker, AiUsageTracker>();
    }
}
