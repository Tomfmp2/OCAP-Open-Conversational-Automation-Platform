using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OCAP.Api.Configuration;
using OCAP.Api.Filters;
using OCAP.Api.Middlewares;
using System.Reflection;
using System.Threading.RateLimiting;
using Asp.Versioning;
using FluentValidation;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Application.Services;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Registry;
using OCAP.Infrastructure.Services;
using OCAP.Infrastructure.Extensions;

namespace OCAP.Api.Extensions;

// Servicios de la capa API. Cada área (seguridad, IA, Google Workspace, OpenAPI) se registra en su propio
// archivo parcial; aquí queda el orden de composición, que importa para los hosted services.
public static partial class ApiServiceExtensions
{
    // Registra todos los servicios propios del gateway HTTP.
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
            {
                // Fail-safe: toda acción requiere autenticación salvo [AllowAnonymous].
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
                options.Filters.Add<FluentValidationActionFilter>();
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problem = new ValidationProblemDetails(context.ModelState)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Validación fallida",
                        Type = "https://httpstatuses.com/400",
                        Instance = context.HttpContext.Request.Path
                    };
                    problem.Extensions["correlationId"] =
                        context.HttpContext.Items[ExceptionHandlingMiddleware.CorrelationHeader];
                    problem.Extensions["requestId"] =
                        context.HttpContext.Items[ExceptionHandlingMiddleware.RequestIdHeader];
                    problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                    return new BadRequestObjectResult(problem)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new HeaderApiVersionReader("X-Api-Version"),
                    new QueryStringApiVersionReader("api-version"));
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = false;
            });

        services.AddEndpointsApiExplorer();

        // Registrar Caché en Memoria
        services.AddMemoryCache();
        services.AddSingleton<IAiResponseCache, InMemoryAiResponseCache>();

        AddSecurityServices(services, configuration);

        services.AddSingleton<IChannelRegistry, ChannelRegistry>();
        services.AddScoped<IChannelConnectionManager, ChannelConnectionManager>();
        services.AddScoped<OCAP.Api.Services.ChannelConversationService>();
        services.AddScoped<IConversationResponder>(sp => sp.GetRequiredService<OCAP.Api.Services.ChannelConversationService>());
        services.AddScoped<OCAP.Api.Queries.DashboardQueries>();
        services.AddScoped<OCAP.Api.Queries.ConversationInboxQueries>();
        services.AddScoped<OCAP.Api.Services.ChannelReplySender>();

        AddAiServices(services, configuration);

        // Registrar Nodos y Motor de Workflow mediante capa de Aplicación
        OCAP.Workflow.Application.DependencyInjection.AddWorkflowApplication(services);
        
        // Registrar servicios de Agentes
        OCAP.Agents.Application.Extensions.AgentServiceExtensions.AddAgentEngineServices(services);

        AddGoogleWorkspaceServices(services, configuration);

        // Registrar missing services
        services.AddScoped<OCAP.Core.Ports.IMessageSender, OCAP.Infrastructure.Services.CoreMessageSender>();

        // Registramos infrastructura de workflows
        OCAP.Workflow.Infrastructure.DependencyInjection.AddWorkflowInfrastructure(services);

        // Swagger / OpenAPI habilitado únicamente en desarrollo.
        AddOpenApiDocumentation(services);

        services.AddOcapObservability(configuration);
        services.AddOcapHealthChecks(configuration);

        services.Configure<CorsSettings>(configuration.GetSection(CorsSettings.SectionName));
        services.Configure<RateLimitingSettings>(configuration.GetSection(RateLimitingSettings.SectionName));

        AddCors(services, configuration);
        AddRateLimiting(services);

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

    private static void AddRateLimiting(IServiceCollection services)
    {
        // Los límites se leen en cada petición (IOptions): así respetan la configuración final del host.
        static RateLimitingSettings Settings(HttpContext context) =>
            context.RequestServices.GetRequiredService<IOptions<RateLimitingSettings>>().Value;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }
                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Demasiadas solicitudes",
                        detail: "Espera un momento antes de volver a intentarlo.")
                    .ExecuteAsync(context.HttpContext);
            };

            // Login, refresh, setup y MFA: pocos intentos por IP aunque el límite global esté apagado.
            options.AddPolicy(RateLimitingSettings.AuthPolicy, context =>
            {
                var limit = Settings(context).AuthPermitLimit;
                return limit <= 0
                    ? RateLimitPartition.GetNoLimiter("auth-off")
                    : RateLimitPartition.GetFixedWindowLimiter($"auth:{ClientKey(context)}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var settings = Settings(context);
                return !settings.EnableRateLimiting
                    ? RateLimitPartition.GetNoLimiter("global-off")
                    : RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        QueueLimit = settings.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });
    }

    // IP del cliente; detrás del panel o de Nginx llega por X-Forwarded-For (ver UseOcapForwardedHeaders).
    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
