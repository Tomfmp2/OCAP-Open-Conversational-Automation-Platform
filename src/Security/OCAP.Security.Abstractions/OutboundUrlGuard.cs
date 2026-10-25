using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace OCAP.Security.Abstractions;

/// <summary>
/// Defensa SSRF para las URLs que configura un usuario (webhooks salientes, Base URL de proveedores de IA): impide que
/// la API haga peticiones a sí misma, a los servicios internos del stack o a la metadata de la nube.
/// </summary>
public static class OutboundUrlGuard
{
    /// <summary>
    /// Valida esquema, credenciales y las IPs a las que resuelve el host. Devuelve el motivo del rechazo o null.
    /// </summary>
    /// <param name="allowPrivateNetwork">
    /// true para destinos legítimos en la red local (Ollama en tu equipo o tu LAN). Aun así quedan fuera loopback,
    /// link-local y los contenedores vecinos de la API.
    /// </param>
    public static async Task<string?> ValidateAsync(string? url, bool allowPrivateNetwork, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return "La URL debe ser absoluta y usar http o https.";
        if (!string.IsNullOrEmpty(uri.UserInfo))
            return "La URL no puede incluir usuario ni contraseña.";

        IPAddress[] addresses;
        try
        {
            addresses = IPAddress.TryParse(uri.DnsSafeHost, out var literal)
                ? [literal]
                : await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (SocketException)
        {
            return $"No se pudo resolver el host '{uri.Host}'.";
        }

        if (addresses.Length == 0) return $"No se pudo resolver el host '{uri.Host}'.";
        return addresses.Any(a => IsBlocked(a, allowPrivateNetwork))
            ? $"El destino '{uri.Host}' apunta a una red interna y no está permitido."
            : null;
    }

    /// <summary>
    /// Handler que vuelve a comprobar cada IP al conectar: la validación previa sola no basta si el DNS cambia entre
    /// la comprobación y la petición (DNS rebinding).
    /// </summary>
    public static SocketsHttpHandler CreateHandler(bool allowPrivateNetwork) => new()
    {
        AllowAutoRedirect = false,
        ConnectCallback = async (context, cancellationToken) =>
        {
            var host = context.DnsEndPoint.Host;
            var addresses = IPAddress.TryParse(host, out var literal)
                ? [literal]
                : await Dns.GetHostAddressesAsync(host, cancellationToken);
            if (addresses.Length == 0 || addresses.Any(a => IsBlocked(a, allowPrivateNetwork)))
                throw new HttpRequestException($"Destino bloqueado por la política de red saliente: {host}");
            var target = addresses[0];

            var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(target, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    };

    public static bool IsBlocked(IPAddress address, bool allowPrivateNetwork)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        // Loopback dentro de un contenedor es la propia API; fuera de Docker es el equipo del usuario (Ollama local).
        if (IPAddress.IsLoopback(address)) return !allowPrivateNetwork || RunningInContainer;
        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) return true;
        if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal) return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            if (b[0] == 0 || b[0] >= 224) return true;                // "esta red", multicast, reservadas y broadcast
            if (b[0] == 169 && b[1] == 254) return true;              // link-local: metadata de AWS, GCP, Azure…
            if (IsPrivateV4(b)) return !allowPrivateNetwork || IsSiblingContainer(address);
            return false;
        }

        // IPv6 de direcciones únicas locales (fc00::/7) = red privada.
        var first = address.GetAddressBytes()[0];
        if ((first & 0xFE) == 0xFC) return !allowPrivateNetwork;
        return false;
    }

    private static readonly bool RunningInContainer =
        string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsPrivateV4(byte[] b) =>
        b[0] == 10
        || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
        || (b[0] == 192 && b[1] == 168)
        || (b[0] == 100 && b[1] >= 64 && b[1] <= 127);           // CGNAT

    /// <summary>
    /// Otro contenedor de las redes de la propia API (Postgres, Redis, Evolution…): misma subred que una interfaz
    /// local y no es su gateway (.1, que en Docker es el equipo anfitrión, donde suele correr Ollama).
    /// </summary>
    private static bool IsSiblingContainer(IPAddress address)
    {
        // Fuera de Docker las interfaces locales son la LAN del usuario, donde un Ollama es legítimo.
        if (!RunningInContainer) return false;

        var target = address.GetAddressBytes();
        foreach (var unicast in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up)
                     .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                     .Where(u => u.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(u.Address)))
        {
            var local = unicast.Address.GetAddressBytes();
            var mask = unicast.IPv4Mask.GetAddressBytes();
            var sameSubnet = true;
            for (var i = 0; i < 4; i++) sameSubnet &= (local[i] & mask[i]) == (target[i] & mask[i]);
            if (!sameSubnet) continue;

            var hostPart = 0u;
            for (var i = 0; i < 4; i++) hostPart = (hostPart << 8) | (uint)(target[i] & ~mask[i] & 0xFF);
            if (hostPart != 1) return true;
        }

        return false;
    }
}
