using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OCAP.Api.Tests.Infrastructure;

/// <summary>Bot API de Telegram en memoria: getMe, setWebhook, getUpdates (cola por token) y sendMessage.</summary>
public sealed class FakeTelegramApi : HttpMessageHandler
{
    public ConcurrentDictionary<string, (string Url, string Secret)> Webhooks { get; } = new();
    public ConcurrentQueue<(string Token, string ChatId, string Text, string? ParseMode)> Sent { get; } = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<object>> _pending = new();
    private long _nextUpdateId = 1000;

    /// <summary>Encola un mensaje privado de texto para el bot con ese token (lo recibe el polling).</summary>
    public long EnqueuePrivateMessage(string token, long chatId, string text) =>
        Enqueue(token, PrivateMessageUpdate(Interlocked.Increment(ref _nextUpdateId), chatId, text));

    public static object PrivateMessageUpdate(long updateId, long chatId, string text, string chatType = "private") => new
    {
        update_id = updateId,
        message = new
        {
            message_id = updateId,
            date = 1761200000,
            text,
            from = new { id = chatId, is_bot = false, first_name = "Ana" },
            chat = new { id = chatId, type = chatType, first_name = "Ana" }
        }
    };

    private long Enqueue(string token, object update)
    {
        _pending.GetOrAdd(token, _ => new ConcurrentQueue<object>()).Enqueue(update);
        return _nextUpdateId;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // https://api.telegram.org/bot{token}/{method}
        var segments = request.RequestUri!.AbsolutePath.Trim('/').Split('/');
        var token = segments[0]["bot".Length..];
        var method = segments[1];
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var json = string.IsNullOrEmpty(body) ? default : JsonDocument.Parse(body).RootElement.Clone();

        switch (method)
        {
            case "getMe":
                return Ok(new { id = 42, is_bot = true, first_name = "OCAP Bot", username = "ocap_test_bot" });
            case "setWebhook":
                Webhooks[token] = (json.GetProperty("url").GetString()!, json.GetProperty("secret_token").GetString()!);
                return Ok(true);
            case "deleteWebhook":
                return Ok(true);
            case "getUpdates":
                var updates = new List<object>();
                if (_pending.TryGetValue(token, out var queue))
                    while (queue.TryDequeue(out var update)) updates.Add(update);
                return Ok(updates);
            case "sendMessage":
                var parseMode = json.TryGetProperty("parse_mode", out var pm) && pm.ValueKind == JsonValueKind.String ? pm.GetString() : null;
                Sent.Enqueue((token, json.GetProperty("chat_id").GetString()!, json.GetProperty("text").GetString()!, parseMode));
                return Ok(new { message_id = 1, date = 1761200000, chat = new { id = 1, type = "private" } });
            default:
                return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    private static HttpResponseMessage Ok(object result) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { ok = true, result }), Encoding.UTF8, "application/json")
        };
}
