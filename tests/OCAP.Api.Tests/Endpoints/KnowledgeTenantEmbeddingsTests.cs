using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

/// <summary>Gemini falso: responde embeddings de 1536 dimensiones y anota la key con que lo llamaron.</summary>
public sealed class FakeGeminiEmbeddings : HttpMessageHandler
{
    public ConcurrentQueue<string> Keys { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
        Keys.Enqueue(query["key"] ?? request.Headers.GetValues("x-goog-api-key").FirstOrDefault() ?? string.Empty);

        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        var count = Math.Max(1, body.Split("\"parts\"").Length - 1);
        var vector = string.Join(",", Enumerable.Range(0, 1536).Select(i => i == 0 ? "1" : "0"));
        var json = request.RequestUri.AbsolutePath.Contains("batchEmbedContents")
            ? $"{{\"embeddings\":[{string.Join(",", Enumerable.Repeat($"{{\"values\":[{vector}]}}", count))}]}}"
            : $"{{\"embedding\":{{\"values\":[{vector}]}}}}";
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}

public sealed class GeminiEmbeddingsFactory : OcapApiFactory
{
    public FakeGeminiEmbeddings Gemini { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.AddHttpClient("GeminiEmbeddingProvider").ConfigurePrimaryHttpMessageHandler(() => Gemini));
    }
}

public class KnowledgeTenantEmbeddingsTests
{
    [Fact]
    public async Task Documents_AreEmbeddedWithTheKeyRegisteredInThePanel()
    {
        using var factory = new GeminiEmbeddingsFactory();
        var client = TestAuthHelper.CreateAuthenticatedClient(factory);

        (await client.PostAsJsonAsync("/api/aiproviderconfigurations", new
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            providerName = "Gemini", displayName = "Gemini", modelName = "gemini-3.5-flash", apiKey = "AIza-clave-de-la-organizacion"
        })).EnsureSuccessStatusCode();

        var kb = await client.PostAsJsonAsync("/api/knowledge", new { name = "Preguntas", description = "FAQ" });
        kb.EnsureSuccessStatusCode();
        var kbId = (await kb.Content.ReadFromJsonAsync<KnowledgeBase>())!.Id;

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("# Horarios\nAbrimos de 9 a 18 h de lunes a viernes.")), "file", "faq.md");
        form.Add(new StringContent(kbId.ToString()), "knowledgeBaseId");
        form.Add(new StringContent("General"), "category");
        var upload = await client.PostAsync("/api/knowledge/upload", form);

        upload.StatusCode.Should().Be(HttpStatusCode.OK, await upload.Content.ReadAsStringAsync());
        factory.Gemini.Keys.Should().NotBeEmpty().And.OnlyContain(k => k == "AIza-clave-de-la-organizacion");
    }

    private sealed record KnowledgeBase(Guid Id);
}
