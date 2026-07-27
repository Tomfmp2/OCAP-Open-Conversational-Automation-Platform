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

        // Registrar servicios de Inteligencia Artificial Generativa y Prompts.
        services.AddSingleton<IAiProvider, MockAiProvider>();
        services.AddSingleton<IPromptBuilder, SystemPromptBuilder>();
        services.AddScoped<IAgentReasoningService, AgentReasoningService>();
        services.AddSingleton<IAiUsageTracker, AiUsageTracker>();

        // Swagger / OpenAPI habilitado únicamente en desarrollo para no exponer la superficie de la API en producción.
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "OCAP API",
                Version = "v1",
                Description = "Open Conversational Automation Platform — API Gateway"
            });
        });

        // Health Checks de ASP.NET Core para verificar el estado del servicio.
        services.AddHealthChecks();

        // Enlaza la sección de configuración de CORS a la clase tipada.
        services.Configure<CorsSettings>(configuration.GetSection(CorsSettings.SectionName));
        services.Configure<RateLimitingSettings>(configuration.GetSection(RateLimitingSettings.SectionName));

        AddCors(services, configuration);
        AddRateLimiting(services, configuration);

        return services;
    }

    // Configura CORS con orígenes explícitos obtenidos de la configuración.
    // Nunca se permite AllowAnyOrigin() porque habilita CSRF desde cualquier dominio.
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
                    // Política restrictiva: solo los orígenes configurados explícitamente.
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
                    // Sin orígenes configurados: bloquear todo CORS por defecto (fail-secure).
                    policy.SetIsOriginAllowed(_ => false);
                }
            });
        });
    }

    // Configura Rate Limiting por IP para proteger el gateway contra consumo excesivo de recursos.
    // Usa la ventana fija de ASP.NET Core nativo (sin Polly ni paquetes adicionales).
    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitSettings = configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>()
            ?? new RateLimitingSettings();

        if (!rateLimitSettings.EnableRateLimiting)
        {
            // Rate limiting deshabilitado: útil en entornos de testing para no interferir con los tests.
            return;
        }

        services.AddRateLimiter(options =>
        {
            // Política de ventana fija: limita peticiones por IP en un período de tiempo.
            options.AddFixedWindowLimiter("IpFixed", limiterOptions =>
            {
                limiterOptions.PermitLimit = rateLimitSettings.PermitLimit;
                limiterOptions.Window = TimeSpan.FromSeconds(rateLimitSettings.WindowSeconds);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = rateLimitSettings.QueueLimit;
            });

            // Responde con 429 Too Many Requests cuando se excede el límite.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }
}
