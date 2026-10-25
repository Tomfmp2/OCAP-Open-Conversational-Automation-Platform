using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace OCAP.Api.Extensions;

public static class ForwardedHeadersExtensions
{
    // Redes de Docker (bridge y compose) y loopback. Una red de la LAN no entra: alguien en ella podría llamar a la API
    // directamente y falsificar su IP con X-Forwarded-For para esquivar el límite de intentos.
    private static readonly string[] DefaultTrustedNetworks = ["172.16.0.0/12", "127.0.0.0/8", "::1/128"];

    /// <summary>Toma la IP del cliente de X-Forwarded-For solo si el salto anterior es un proxy de confianza.</summary>
    public static WebApplication UseOcapForwardedHeaders(this WebApplication app, IConfiguration configuration)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            // Solo el último salto: lo que el cliente ponga antes en la cabecera no cuenta.
            ForwardLimit = 1
        };
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        var networks = configuration.GetSection("ForwardedHeaders:TrustedNetworks").Get<string[]>();
        foreach (var network in networks is { Length: > 0 } ? networks : DefaultTrustedNetworks)
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }

        app.UseForwardedHeaders(options);
        return app;
    }
}
