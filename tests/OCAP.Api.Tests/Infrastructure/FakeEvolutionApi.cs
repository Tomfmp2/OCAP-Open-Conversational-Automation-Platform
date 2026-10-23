using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OCAP.Api.Tests.Infrastructure;

/// <summary>Evolution API v2 en memoria: instancias, estado de conexión y mensajes enviados.</summary>
public sealed class FakeEvolutionApi : HttpMessageHandler
{
    public ConcurrentDictionary<string, string> Instances { get; } = new();
    public ConcurrentDictionary<string, JsonElement> Webhooks { get; } = new();
    public ConcurrentQueue<(string Instance, string Number, string Text)> SentTexts { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var segments = request.RequestUri!.AbsolutePath.Trim('/').Split('/');
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var json = string.IsNullOrEmpty(body) ? default : JsonDocument.Parse(body).RootElement.Clone();

        switch (segments)
        {
            case ["instance", "create"]:
                var created = json.GetProperty("instanceName").GetString()!;
                Instances[created] = "close";
                Webhooks[created] = json.GetProperty("webhook");
                return Json(HttpStatusCode.Created, new { instance = new { instanceName = created, status = "created" } });
            case ["webhook", "set", var instance]:
                Webhooks[instance] = json.GetProperty("webhook");
                return Json(HttpStatusCode.Created, new { });
            case ["instance", "connectionState", var instance]:
                return Instances.TryGetValue(instance, out var state)
                    ? Json(HttpStatusCode.OK, new { instance = new { instanceName = instance, state } })
                    : Json(HttpStatusCode.NotFound, new { status = 404, error = "Not Found" });
            case ["instance", "connect", var instance]:
                if (Instances.TryGetValue(instance, out var current) && current == "open")
                    return Json(HttpStatusCode.OK, new { instance = new { instanceName = instance, state = "open" } });
                Instances[instance] = "connecting";
                return Json(HttpStatusCode.OK, new { pairingCode = (string?)null, code = "2@qr", base64 = "data:image/png;base64,QR", count = 1 });
            case ["instance", "logout", var instance]:
                Instances[instance] = "close";
                return Json(HttpStatusCode.OK, new { status = "SUCCESS" });
            case ["instance", "delete", var instance]:
                Instances.TryRemove(instance, out _);
                Webhooks.TryRemove(instance, out _);
                return Json(HttpStatusCode.OK, new { status = "SUCCESS" });
            case ["message", "sendText", var instance]:
                SentTexts.Enqueue((instance, json.GetProperty("number").GetString()!, json.GetProperty("text").GetString()!));
                return Json(HttpStatusCode.Created, new { key = new { id = Guid.NewGuid().ToString("N") } });
            case ["instance", "fetchInstances"]:
                return Json(HttpStatusCode.OK, Array.Empty<object>());
            default:
                return Json(HttpStatusCode.NotFound, new { status = 404 });
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object payload) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
}
