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
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Registry;
using OCAP.Infrastructure.Services;
using OCAP.Workflow.Application.Services;

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
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IConsentService, ConsentService>();
        services.AddOcapOpenIddict();
        services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IExternalIdentityResolver, ExternalIdentityResolver>();
        services.AddSingleton<ICredentialVault, AesDbCredentialVault>();
        services.AddSingleton<IChannelRegistry, ChannelRegistry>();
        services.AddScoped<IChannelConnectionManager, ChannelConnectionManager>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();

        services.AddScoped<AuthenticateUserUseCase>();
        services.AddScoped<CreateTenantUseCase>();
        services.AddScoped<CreateApiKeyUseCase>();
        services.AddScoped<RevokeApiKeyUseCase>();

        // Registrar Webhooks Platform (HMAC SHA-256, Delivery Engine y Suscriptor de Event Bus)
        services.AddSingleton<IWebhookSigner, HmacSha256WebhookSigner>();
        services.AddHttpClient<IWebhookService, WebhookService>();
        services.AddHostedService<WebhookEventSubscriber>();

        // Registrar Live Gateway (SignalR e integración con IEventBus para streaming en vivo)
        services.AddSignalR();
        services.AddHostedService<OCAP.Api.Services.LiveGatewayEventSubscriber>();

        // Registrar clientes HTTP y proveedores de IA Generativa con Resilience (Polly).
        services.AddHttpClient<OpenAiProvider>().AddStandardResilienceHandler();
        services.AddHttpClient<GeminiAiProvider>().AddStandardResilienceHandler();
        services.AddHttpClient<OllamaAiProvider>().AddStandardResilienceHandler();

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

        // Registro de Proveedores de IA y Servicio de Configuración por Tenant
        services.AddSingleton<IAiProviderRegistry, AiProviderRegistry>();
        services.AddScoped<IAiProviderConfigurationService, AiProviderConfigurationService>();

        // Orquestador inteligente de proveedores
        services.AddSingleton<IAiProviderSelector, AiProviderSelector>();

        // Prompts y razonamiento
        services.AddSingleton<IPromptBuilder, SystemPromptBuilder>();
        services.AddScoped<IAgentReasoningService, AgentReasoningService>();
        services.AddSingleton<IAiUsageTracker, AiUsageTracker>();

        // Registrar Nodos y Motor de Workflow mediante capa de Aplicación
        OCAP.Workflow.Application.DependencyInjection.AddWorkflowApplication(services);
        
        services.AddSingleton<OCAP.Tools.Abstractions.IToolRegistry, OCAP.Agents.Application.Services.ToolRegistry>();
        services.AddScoped<OCAP.Agents.Abstractions.Ports.IAgentRepository, OCAP.Agents.Application.Persistence.Repositories.AgentRepository>();
        
        // Registrar missing services
        services.AddScoped<OCAP.Agents.Abstractions.Ports.IActionDispatcher, OCAP.Agents.Application.Services.ActionDispatcher>();
        services.AddSingleton<OCAP.Core.Ports.IMessageSender, OCAP.Infrastructure.Services.CoreMessageSenderMock>();
        services.AddSingleton<OCAP.Security.Abstractions.IPermissionValidator, OCAP.Security.Abstractions.DefaultPermissionValidator>();

        // Registramos infrastructura de workflows
        OCAP.Workflow.Infrastructure.DependencyInjection.AddWorkflowInfrastructure(services);

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

        services.AddHealthChecks()
            .AddDbContextCheck<OCAP.Infrastructure.Persistence.Context.OCAPDbContext>("Database");

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
