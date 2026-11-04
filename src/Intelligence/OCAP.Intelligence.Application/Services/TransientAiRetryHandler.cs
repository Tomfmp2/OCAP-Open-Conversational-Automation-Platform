using System.Net;

namespace OCAP.Intelligence.Application.Services;

/// <summary>
/// Reintenta las caídas momentáneas de un proveedor de IA (502, 503, 504 o error de red). No reintenta un 429: es cuota
/// agotada y otra petición solo la gastaría. Los modelos del panel comparten un cliente por organización y destino
/// variable, por eso no se usa un circuit breaker común: la cuota de una organización cortaría a las demás.
/// </summary>
public sealed class TransientAiRetryHandler : DelegatingHandler
{
    private static readonly TimeSpan[] DefaultDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];

    private readonly IReadOnlyList<TimeSpan> _delays;

    public TransientAiRetryHandler(HttpMessageHandler innerHandler, IReadOnlyList<TimeSpan>? delays = null)
        : base(innerHandler)
    {
        _delays = delays ?? DefaultDelays;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        for (var attempt = 0; ; attempt++)
        {
            var isLast = attempt >= _delays.Count;
            try
            {
                var response = await base.SendAsync(attempt == 0 ? request : Clone(request, body), cancellationToken);
                if (isLast || !IsTransient(response.StatusCode)) return response;
                response.Dispose();
            }
            catch (HttpRequestException) when (!isLast)
            {
                // Se reintenta abajo.
            }

            await Task.Delay(_delays[attempt], cancellationToken);
        }
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    private static HttpRequestMessage Clone(HttpRequestMessage request, byte[]? body)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        foreach (var header in request.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (body is not null)
        {
            clone.Content = new ByteArrayContent(body);
            foreach (var header in request.Content!.Headers) clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }
}
