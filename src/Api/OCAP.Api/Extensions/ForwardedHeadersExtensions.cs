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

        foreach (var network in TrustedNetworks(configuration)) options.KnownIPNetworks.Add(network);

        app.UseForwardedHeaders(options);
        return app;
    }

    /// <summary>
    /// /metrics expone el detalle interno de la API (rutas, tiempos, contadores): solo lo lee Prometheus desde la
    /// red de Docker. Con acceso LAN activado, un equipo de la red recibe 404.
    /// </summary>
    public static WebApplication UseInternalOnlyMetrics(this WebApplication app, IConfiguration configuration)
    {
        var networks = TrustedNetworks(configuration);
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/metrics"),
            branch => branch.Use(async (context, next) =>
            {
                var ip = context.Connection.RemoteIpAddress;
                if (ip is null || !networks.Any(n => n.Contains(ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip)))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                await next();
            }));
        return app;
    }

    private static List<System.Net.IPNetwork> TrustedNetworks(IConfiguration configuration)
    {
        var configured = configuration.GetSection("ForwardedHeaders:TrustedNetworks").Get<string[]>();
        return (configured is { Length: > 0 } ? configured : DefaultTrustedNetworks)
            .Select(System.Net.IPNetwork.Parse)
            .ToList();
    }
}
