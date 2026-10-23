using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OCAP.Channels.WhatsApp.Configuration;
using OCAP.Channels.WhatsApp.Evolution;

namespace OCAP.Channels.WhatsApp.Tests.Evolution;

public class EvolutionApiClientTests
{
    private readonly List<(HttpMethod Method, string Path, string? Body, string? ApiKey)> _requests = new();
    private Func<HttpRequestMessage, HttpResponseMessage> _respond = _ => Json(HttpStatusCode.OK, "{}");

    private EvolutionApiClient CreateClient() => new(
        new HttpClient(new StubHandler(this)),
        Options.Create(new WhatsAppSettings { BaseUrl = "http://evolution:8080", ApiKey = "global-key" }),
        NullLogger<EvolutionApiClient>.Instance);

    [Fact]
    public async Task SendText_UsesTheV2Contract()
    {
        await CreateClient().SendTextAsync("ocap-abc", "573001234567", "Hola", CancellationToken.None);

        var request = _requests.Single();
        request.Method.Should().Be(HttpMethod.Post);
        request.Path.Should().Be("/message/sendText/ocap-abc");
        request.ApiKey.Should().Be("global-key");
        JsonDocument.Parse(request.Body!).RootElement.ToString().Should().Be("""{"number":"573001234567","text":"Hola"}""");
    }

    [Fact]
    public async Task CreateInstance_SubscribesTheWebhookWithItsSignatureHeader()
    {
        var webhook = new EvolutionWebhookConfig("http://ocap-api:5000/api/webhooks/whatsapp", new Dictionary<string, string> { ["X-Sig"] = "firma" });

        await CreateClient().CreateInstanceAsync("ocap-abc", webhook, CancellationToken.None);

        var request = _requests.Single();
        request.Path.Should().Be("/instance/create");
        var body = JsonDocument.Parse(request.Body!).RootElement;
        body.GetProperty("instanceName").GetString().Should().Be("ocap-abc");
        body.GetProperty("integration").GetString().Should().Be("WHATSAPP-BAILEYS");
        var hook = body.GetProperty("webhook");
        hook.GetProperty("url").GetString().Should().Be(webhook.Url);
        hook.GetProperty("headers").GetProperty("X-Sig").GetString().Should().Be("firma");
        hook.GetProperty("events").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo("QRCODE_UPDATED", "CONNECTION_UPDATE", "MESSAGES_UPSERT");
    }

    [Fact]
    public async Task ConnectionState_OfAnUnknownInstance_IsMissing()
    {
        _respond = _ => Json(HttpStatusCode.NotFound, """{"status":404,"error":"Not Found"}""");

        var state = await CreateClient().GetConnectionStateAsync("ocap-abc", CancellationToken.None);

        state.Should().Be(EvolutionStates.Missing);
    }

    [Fact]
    public async Task ConnectionState_ReadsTheNestedState()
    {
        _respond = _ => Json(HttpStatusCode.OK, """{"instance":{"instanceName":"ocap-abc","state":"open"}}""");

        (await CreateClient().GetConnectionStateAsync("ocap-abc", CancellationToken.None)).Should().Be(EvolutionStates.Open);
    }

    [Fact]
    public async Task Connect_ReturnsTheQr()
    {
        _respond = _ => Json(HttpStatusCode.OK, """{"pairingCode":null,"code":"2@abc","base64":"data:image/png;base64,AAA","count":1}""");

        var result = await CreateClient().ConnectAsync("ocap-abc", CancellationToken.None);

        result.State.Should().Be(EvolutionStates.Connecting);
        result.Qr.Should().Be(new EvolutionQrCode("data:image/png;base64,AAA", "2@abc", null));
    }

    [Fact]
    public async Task Connect_WhenAlreadyLinked_ReturnsOpenWithoutQr()
    {
        _respond = _ => Json(HttpStatusCode.OK, """{"instance":{"instanceName":"ocap-abc","state":"open"}}""");

        var result = await CreateClient().ConnectAsync("ocap-abc", CancellationToken.None);

        result.Should().Be(new EvolutionConnectResult(EvolutionStates.Open, null));
    }

    [Fact]
    public async Task Failure_IsThrownWithItsStatusCode()
    {
        _respond = _ => Json(HttpStatusCode.Unauthorized, """{"status":401,"error":"Unauthorized"}""");

        var act = () => CreateClient().SendTextAsync("ocap-abc", "573001234567", "Hola", CancellationToken.None);

        (await act.Should().ThrowAsync<EvolutionApiException>()).Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task DeletingAnInstanceThatNoLongerExists_IsNotAnError()
    {
        _respond = _ => Json(HttpStatusCode.NotFound, "{}");

        var act = () => CreateClient().DeleteInstanceAsync("ocap-abc", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    private sealed class StubHandler(EvolutionApiClientTests test) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var apiKey = request.Headers.TryGetValues("apikey", out var values) ? values.Single() : null;
            test._requests.Add((request.Method, request.RequestUri!.AbsolutePath, body, apiKey));
            return test._respond(request);
        }
    }
}
