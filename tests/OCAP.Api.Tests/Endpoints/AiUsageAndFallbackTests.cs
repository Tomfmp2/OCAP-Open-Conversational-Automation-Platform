using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Tests.Infrastructure;
using OCAP.Intelligence.Abstractions;
using OCAP.Intelligence.Application.Services;

namespace OCAP.Api.Tests.Endpoints;

/// <summary>Los canales ante un proveedor sin cuota o sin respuesta, y el registro real de lo consumido.</summary>
public class AiUsageAndFallbackTests
{
    private const string Reply = """{"candidates":[{"content":{"parts":[{"text":"respuesta de lite"}]}}],"usageMetadata":{"totalTokenCount":50}}""";
    private const string NoQuota = """{"error":{"code":429,"details":[{"retryDelay":"3600s"}]}}""";

    private sealed class GeminiStub(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class Factory(Func<HttpRequestMessage, HttpResponseMessage> respond) : OcapApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services => services.AddSingleton<IAiProviderRegistry>(sp =>
                new AiProviderRegistry(sp.GetServices<IAiProvider>(), new HttpClient(new GeminiStub(respond)))));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static async Task<Guid> AddModelAsync(HttpClient admin, string model)
    {
        var created = await admin.PostAsJsonAsync("/api/aiproviderconfigurations", new
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            providerName = "Gemini", displayName = model, modelName = model, apiKey = "AIza-no-real",
        });
        created.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> ConnectWebChatAsync(HttpClient admin, Guid modelId)
    {
        (await admin.PostAsJsonAsync("/api/channels/webchat/connect", new { displayName = "Sitio", widgetTitle = "Hola" })).EnsureSuccessStatusCode();
        var channel = JsonDocument.Parse(await admin.GetStringAsync("/api/channels/connections")).RootElement.GetProperty("data")[0];
        (await admin.PutAsJsonAsync($"/api/channels/connections/{channel.GetProperty("id").GetGuid()}/ai", new { aiProviderConfigId = modelId })).EnsureSuccessStatusCode();
        return channel.GetProperty("configurationMetadata").GetProperty("PublicKey").GetString()!;
    }

    private static async Task<string?> AskAsync(HttpClient visitor, string key)
    {
        var sent = await visitor.PostAsJsonAsync($"/api/public/webchat/{key}/messages", new { sessionId = Guid.NewGuid(), message = "hola" });
        sent.StatusCode.Should().Be(HttpStatusCode.OK, await sent.Content.ReadAsStringAsync());
        return JsonDocument.Parse(await sent.Content.ReadAsStringAsync()).RootElement.GetProperty("reply").GetString();
    }

    [Fact]
    public async Task SinCuotaEnElModeloDelCanal_RespondeOtroModeloYQuedaRegistrado()
    {
        using var factory = new Factory(request => request.RequestUri!.AbsoluteUri.Contains("flash-sin-cuota")
            ? Json(HttpStatusCode.TooManyRequests, NoQuota)
            : Json(HttpStatusCode.OK, Reply));
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        var exhausted = await AddModelAsync(admin, "flash-sin-cuota");
        await AddModelAsync(admin, "lite-disponible");
        var key = await ConnectWebChatAsync(admin, exhausted);

        var reply = await AskAsync(factory.CreateClient(), key);

        reply.Should().Be("respuesta de lite");
        var usage = JsonDocument.Parse(await admin.GetStringAsync("/api/ai/usage")).RootElement;
        usage.GetProperty("totalTokensUsed").GetInt32().Should().Be(50);
        var models = usage.GetProperty("lastDay").EnumerateArray().ToDictionary(m => m.GetProperty("model").GetString()!);
        models["flash-sin-cuota"].GetProperty("failed").GetInt32().Should().Be(1);
        models["lite-disponible"].GetProperty("tokens").GetInt64().Should().Be(50);
    }

    [Fact]
    public async Task SiElProveedorNoContestaATiempo_ElClienteRecibeLaRespuestaDeRespaldo()
    {
        // Regresión: el plazo vencido se trataba como cancelación del cliente y la API devolvía 500 sin responderle.
        using var factory = new Factory(_ => throw new TaskCanceledException("plazo vencido"));
        var admin = TestAuthHelper.CreateAuthenticatedClient(factory);
        var model = await AddModelAsync(admin, "lento");
        var key = await ConnectWebChatAsync(admin, model);

        var reply = await AskAsync(factory.CreateClient(), key);

        reply.Should().Contain("no está disponible");
    }
}
