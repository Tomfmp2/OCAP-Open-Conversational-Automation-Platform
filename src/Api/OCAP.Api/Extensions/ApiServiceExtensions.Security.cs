using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OCAP.Api.Security;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.Options;
using OCAP.Security.Application.UseCases;
using OCAP.Security.Infrastructure.Configuration;
using OCAP.Security.Infrastructure.Services;

namespace OCAP.Api.Extensions;

public static partial class ApiServiceExtensions
{
    /// <summary>Autenticación (JWT, OpenIddict), identidades, MFA, SSO, directorio, vault, webhooks y SignalR.</summary>
    private static void AddSecurityServices(IServiceCollection services, IConfiguration configuration)
    {
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
        services.AddScoped<InitialAdminProvisioner>();
        // Orden: el bootstrap opcional por entorno corre antes que el código de configuración inicial.
        services.AddHostedService<BootstrapAdminHostedService>();
        services.AddSingleton<OCAP.Api.Setup.SetupCodeService>();
        services.AddHostedService(sp => sp.GetRequiredService<OCAP.Api.Setup.SetupCodeService>());
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IConsentService, ConsentService>();

        AddJwtBearerAuthentication(services, jwtOptions);
        services.AddAuthorization(options => options.AddOcapPermissionPolicies());
        services.AddOcapOpenIddict();

        services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IExternalIdentityResolver, ExternalIdentityResolver>();
        services.AddSingleton<ICredentialVault, AesDbCredentialVault>();
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
        // Una sola instancia por scope: ITenantScope fija el tenant que luego lee ITenantContext (y EF).
        services.AddScoped<HttpTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<HttpTenantContext>());
        services.AddScoped<ITenantScope>(sp => sp.GetRequiredService<HttpTenantContext>());
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
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrWhiteSpace(accessToken)
                            && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
    }
}
