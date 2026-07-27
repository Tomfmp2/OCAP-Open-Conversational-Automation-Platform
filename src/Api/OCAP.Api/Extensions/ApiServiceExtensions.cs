using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OCAP.Api.Configuration;
using System.Threading.RateLimiting;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Application.Services;
using OCAP.Intelligence.Mock;
using OCAP.Prompts;
using OCAP.Providers.Gemini;
using OCAP.Providers.Ollama;
using OCAP.Providers.OpenAI;
using OCAP.Security.Abstractions;
using OCAP.Security.Application.UseCases;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Api.Extensions;

// Extensión para configurar servicios específicos de la capa API.
// Aplica el principio de Separation of Concerns: cada aspecto de configuración queda aislado.
public static class ApiServiceExtensions
{
    // Registra todos los servicios propios del gateway HTTP.
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        // Registrar Caché en Memoria
        services.AddMemoryCache();
        services.AddSingleton<IAiResponseCache, InMemoryAiResponseCache>();

        // Registrar servicios de Seguridad, Autenticación y Multi-Tenant.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService>(sp => new JwtTokenService(
            configuration["Jwt:SecretKey"] ?? "OCAP_SUPER_SECRET_SECURITY_KEY_FOR_JWT_SIGNING_2026_PRODUCTION"));
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddSingleton<ISecurityAuditService, SecurityAuditService>();

        services.AddScoped<AuthenticateUserUseCase>();
        services.AddScoped<CreateTenantUseCase>();
        services.AddScoped<CreateApiKeyUseCase>();

        // Registrar clientes HTTP y proveedores de IA Generativa.
        services.AddHttpClient<OpenAiProvider>();
        services.AddHttpClient<GeminiAiProvider>();
        services.AddHttpClient<OllamaAiProvider>();

        var openAiSettings = new AiProviderSettings
        {
            ApiKey = configuration["AiProviders:OpenAI:ApiKey"] ?? string.Empty,
            BaseUrl = configuration["AiProviders:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1",
            ModelName = configuration["AiProviders:OpenAI:ModelName"] ?? "gpt-4o"
        };

        var geminiSettings = new AiProviderSettings
        {
            ApiKey = configuration["AiProviders:Gemini:ApiKey"] ?? string.Empty,
            ModelName = configuration["AiProviders:Gemini:ModelName"] ?? "gemini-1.5-flash"
        };

        var ollamaSettings = new AiProviderSettings
        {
            BaseUrl = configuration["AiProviders:Ollama:BaseUrl"] ?? "http://localhost:11434",
            ModelName = configuration["AiProviders:Ollama:ModelName"] ?? "llama3"
        };

        services.AddSingleton<IAiProvider>(sp => new OpenAiProvider(sp.GetRequiredService<HttpClient>(), openAiSettings));
        services.AddSingleton<IAiProvider>(sp => new GeminiAiProvider(sp.GetRequiredService<HttpClient>(), geminiSettings));
        services.AddSingleton<IAiProvider>(sp => new OllamaAiProvider(sp.GetRequiredService<HttpClient>(), ollamaSettings));
        services.AddSingleton<IAiProvider, MockAiProvider>();

        // Orquestador inteligente de proveedores
        services.AddSingleton<IAiProviderSelector, AiProviderSelector>();

        // Prompts y razonamiento
        services.AddSingleton<IPromptBuilder, SystemPromptBuilder>();
        services.AddScoped<IAgentReasoningService, AgentReasoningService>();
        services.AddSingleton<IAiUsageTracker, AiUsageTracker>();

        // Swagger / OpenAPI habilitado únicamente en desarrollo.
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "OCAP API",
                Version = "v1",
                Description = "Open Conversational Automation Platform — API Gateway"
            });
        });

        services.AddHealthChecks();

        services.Configure<CorsSettings>(configuration.GetSection(CorsSettings.SectionName));
        services.Configure<RateLimitingSettings>(configuration.GetSection(RateLimitingSettings.SectionName));

        AddCors(services, configuration);
        AddRateLimiting(services, configuration);

        return services;
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()
            ?? new CorsSettings();

        services.AddCors(options =>
        {
            options.AddPolicy("OcapCorsPolicy", policy =>
            {
                if (corsSettings.AllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(corsSettings.AllowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();

                    if (corsSettings.AllowCredentials)
                    {
                        policy.AllowCredentials();
                    }
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => false);
                }
            });
        });
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitSettings = configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>()
            ?? new RateLimitingSettings();

        if (!rateLimitSettings.EnableRateLimiting)
        {
            return;
        }

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("IpFixed", limiterOptions =>
            {
                limiterOptions.PermitLimit = rateLimitSettings.PermitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(rateLimitSettings.WindowSeconds);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = rateLimitSettings.QueueLimit;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }
}
