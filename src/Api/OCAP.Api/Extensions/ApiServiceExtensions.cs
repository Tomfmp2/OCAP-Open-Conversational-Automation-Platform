using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OCAP.Api.Configuration;
using System.Text;
using System.Threading.RateLimiting;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Application.Services;

using OCAP.Prompts;
using OCAP.Providers.Gemini;
using OCAP.Providers.Claude;
using OCAP.Providers.Ollama;
using OCAP.Providers.OpenAI;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.Options;
using OCAP.Security.Application.UseCases;
using OCAP.Security.Infrastructure.Configuration;
using OCAP.Security.Infrastructure.Services;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Nodes;
using OCAP.Channels.Abstractions.Contracts;
using OCAP.Channels.Abstractions.Registry;
using OCAP.Infrastructure.Services;
using OCAP.Workflow.Application.Services;
using OCAP.Providers.Google.Abstractions;
using OCAP.Providers.Google.Calendar;
using OCAP.Providers.Google.Gmail;
using OCAP.Providers.Google.Sheets;
using OCAP.Tools.Google;
using OCAP.Tools.Abstractions;
using OCAP.Infrastructure.Extensions;

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

        // Secretos y autenticación (PI-1)
        var jwtOptions = SecurityConfigurationBinder.BindJwtOptions(configuration);
        var vaultOptions = SecurityConfigurationBinder.BindVaultOptions(configuration);

        services.Configure<JwtOptions>(options =>
        {
            options.SecretKey = jwtOptions.SecretKey;
            options.Issuer = jwtOptions.Issuer;
            options.Audience = jwtOptions.Audience;
            options.AccessTokenExpiryMinutes = jwtOptions.AccessTokenExpiryMinutes;
        });
        services.Configure<VaultOptions>(options =>
        {
            options.MasterKey = vaultOptions.MasterKey;
        });

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService>(_ => new JwtTokenService(jwtOptions));
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IUserAuthenticationQuery, EfUserAuthenticationQuery>();
        services.AddHostedService<BootstrapAdminHostedService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IConsentService, ConsentService>();

        AddJwtBearerAuthentication(services, jwtOptions);
        services.AddAuthorization();
        services.AddOcapOpenIddict();

        services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IExternalIdentityResolver, ExternalIdentityResolver>();
        services.AddSingleton<ICredentialVault, AesDbCredentialVault>();
        services.AddSingleton<IChannelRegistry, ChannelRegistry>();
        services.AddScoped<IChannelConnectionManager, ChannelConnectionManager>();

        // Registrar Proveedores de Identidad Externos (CAP-15)
        services.Configure<OCAP.Security.Abstractions.DTOs.ExternalAuthenticationSettings>(configuration.GetSection("Authentication"));
        services.AddHttpClient<OCAP.Security.Infrastructure.Services.Providers.GoogleExternalAuthProvider>();
        services.AddHttpClient<OCAP.Security.Infrastructure.Services.Providers.MicrosoftExternalAuthProvider>();
        services.AddHttpClient<OCAP.Security.Infrastructure.Services.Providers.GitHubExternalAuthProvider>();
        services.AddHttpClient<OCAP.Security.Infrastructure.Services.Providers.GenericOidcExternalAuthProvider>();

        services.AddScoped<IExternalAuthProvider, OCAP.Security.Infrastructure.Services.Providers.GoogleExternalAuthProvider>();
        services.AddScoped<IExternalAuthProvider, OCAP.Security.Infrastructure.Services.Providers.MicrosoftExternalAuthProvider>();
        services.AddScoped<IExternalAuthProvider, OCAP.Security.Infrastructure.Services.Providers.GitHubExternalAuthProvider>();
        services.AddScoped<IExternalAuthProvider, OCAP.Security.Infrastructure.Services.Providers.GenericOidcExternalAuthProvider>();
        services.AddScoped<IExternalAuthenticationService, ExternalAuthenticationService>();

        // Registrar servicios de administración de identidades (CAP-16)
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IGroupService, GroupService>();

        // Registrar MFA (TOTP / Recovery Codes) y WebAuthn / Passkeys (CAP-17)
        services.AddSingleton<ITotpService, TotpService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IWebAuthnService, WebAuthnService>();

        // Registrar Enterprise Single Sign-On SAML 2.0 (CAP-18)
        services.AddScoped<ISamlService, SamlService>();

        // Registrar Enterprise Directory Synchronization (SCIM 2.0 & LDAP / Active Directory) (CAP-19)
        services.AddScoped<IScimService, ScimService>();
        services.AddScoped<ILdapService, LdapService>();
        services.AddScoped<IDirectorySyncEngine, DirectorySyncEngine>();
        services.AddHostedService<DirectorySyncBackgroundService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<IUserContext, HttpUserContext>();

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
        services.AddHttpClient("Claude").AddStandardResilienceHandler();

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

        var claudeSettings = new AiProviderSettings
        {
            ApiKey = configuration["AiProviders:Claude:ApiKey"] ?? string.Empty,
            BaseUrl = configuration["AiProviders:Claude:BaseUrl"] ?? "https://api.anthropic.com/v1",
            ModelName = configuration["AiProviders:Claude:ModelName"] ?? "claude-3-5-sonnet-latest"
        };

        services.AddSingleton<IAiProvider>(sp => new OpenAiProvider(sp.GetRequiredService<HttpClient>(), openAiSettings));
        services.AddSingleton<IAiProvider>(sp => new GeminiAiProvider(sp.GetRequiredService<HttpClient>(), geminiSettings));
        services.AddSingleton<IAiProvider>(sp => new OllamaAiProvider(sp.GetRequiredService<HttpClient>(), ollamaSettings));
        services.AddSingleton<IAiProvider>(sp => new ClaudeAiProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("Claude"),
            claudeSettings));

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
        
        // Registrar servicios de Agentes
        OCAP.Agents.Application.Extensions.AgentServiceExtensions.AddAgentEngineServices(services);

        // Google Workspace usa adaptadores reales cuando hay token. En producción sin
        // credenciales también se registran los adaptadores HTTP para evitar éxitos ficticios.
        var googleSection = configuration.GetSection(GoogleWorkspaceOptions.SectionName);
        var googleOptions = googleSection.Get<GoogleWorkspaceOptions>() ?? new GoogleWorkspaceOptions();
        services.Configure<GoogleWorkspaceOptions>(googleSection);

        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Production;
        var isDevelopmentOrTesting =
            environmentName.Equals(Environments.Development, StringComparison.OrdinalIgnoreCase) ||
            environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase);
        var useInMemoryGoogle =
            googleOptions.UseInMemory ||
            (string.IsNullOrWhiteSpace(googleOptions.AccessToken) && isDevelopmentOrTesting);

        if (useInMemoryGoogle)
        {
            services.AddSingleton<ICalendarProvider, InMemoryCalendarProvider>();
            services.AddSingleton<IEmailProvider, InMemoryEmailProvider>();
            services.AddSingleton<ISpreadsheetProvider, InMemorySpreadsheetProvider>();
        }
        else
        {
            services.AddHttpClient<ICalendarProvider, GoogleCalendarHttpProvider>()
                .AddStandardResilienceHandler();
            services.AddHttpClient<IEmailProvider, GoogleGmailHttpProvider>()
                .AddStandardResilienceHandler();
            services.AddHttpClient<ISpreadsheetProvider, GoogleSheetsHttpProvider>()
                .AddStandardResilienceHandler();
        }

        // Registrar Google Tools en DI
        services.AddTransient<ITool, CreateCalendarEventTool>();
        services.AddTransient<ITool, SendEmailTool>();
        services.AddTransient<ITool, AppendSpreadsheetRowTool>();

        // Registrar missing services
        services.AddScoped<OCAP.Core.Ports.IMessageSender, OCAP.Infrastructure.Services.CoreMessageSender>();

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

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddOcapObservability(configuration);
        services.AddOcapHealthChecks(configuration);

        services.Configure<CorsSettings>(configuration.GetSection(CorsSettings.SectionName));
        services.Configure<RateLimitingSettings>(configuration.GetSection(RateLimitingSettings.SectionName));

        AddCors(services, configuration);
        AddRateLimiting(services, configuration);

        return services;
    }

    private static void AddJwtBearerAuthentication(IServiceCollection services, JwtOptions jwtOptions)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
                };
            });
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
