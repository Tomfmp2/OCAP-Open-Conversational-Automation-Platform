using Microsoft.Extensions.DependencyInjection;
using OCAP.Infrastructure.Persistence.Context;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace OCAP.Api.Extensions;

// Configuración de OpenIddict como Servidor de Autorización OAuth2/OpenID Connect para OCAP
public static class OpenIddictExtensions
{
    public static IServiceCollection AddOcapOpenIddict(this IServiceCollection services)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<OCAPDbContext>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token")
                       .SetAuthorizationEndpointUris("/connect/authorize");

                options.AllowClientCredentialsFlow()
                       .AllowRefreshTokenFlow();

                // Certificados de firma y cifrado de desarrollo para Tokens JWT / OIDC
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .EnableAuthorizationEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
