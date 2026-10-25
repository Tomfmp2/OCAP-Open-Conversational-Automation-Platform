using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using OCAP.Api.Tests.Infrastructure;

namespace OCAP.Api.Tests.Endpoints;

public class KnowledgeUploadTests : IClassFixture<OcapApiFactory>
{
    private readonly OcapApiFactory _factory;

    public KnowledgeUploadTests(OcapApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("malware.exe", "MZ\u0090\u0000")]                   // extensión no admitida
    [InlineData("informe.pdf", "MZ\u0090\u0000 no soy un pdf")]     // ejecutable renombrado
    [InlineData("notas.txt", "texto\u0000con\u0000nulos")]          // binario con extensión de texto
    public async Task UnsafeFiles_AreRejected(string fileName, string content)
    {
        var response = await Upload(fileName, Encoding.Latin1.GetBytes(content));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Archivo no válido");
    }

    [Fact]
    public async Task ATextDocument_PassesValidation()
    {
        var response = await Upload("preguntas.md", Encoding.UTF8.GetBytes("# Horarios\nAbrimos de 9 a 18 h. ¿Envíos? Sí."));

        // Sin proveedor de embeddings en las pruebas la indexación puede fallar después; aquí importa que el archivo
        // no se rechace por su tipo o contenido.
        (await response.Content.ReadAsStringAsync()).Should().NotContain("Archivo no válido");
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
    }

    private async Task<HttpResponseMessage> Upload(string fileName, byte[] bytes)
    {
        var client = TestAuthHelper.CreateAuthenticatedClient(_factory);
        var kb = await client.PostAsJsonAsync("/api/knowledge", new { name = $"KB {Guid.NewGuid():N}", description = "pruebas" });
        kb.EnsureSuccessStatusCode();
        var kbId = (await kb.Content.ReadFromJsonAsync<KnowledgeBase>())!.Id;

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        form.Add(new StringContent(kbId.ToString()), "knowledgeBaseId");
        form.Add(new StringContent("General"), "category");
        return await client.PostAsync("/api/knowledge/upload", form);
    }

    private sealed record KnowledgeBase(Guid Id);
}
