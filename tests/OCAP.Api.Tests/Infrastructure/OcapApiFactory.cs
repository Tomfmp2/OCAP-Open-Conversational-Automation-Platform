using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace OCAP.Api.Tests.Infrastructure;

// Factory personalizada para levantar la API completa en memoria durante las pruebas.
// Sustituye la base de datos PostgreSQL real por InMemory mediante la configuración UseInMemory.
public class OcapApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Establece el entorno como Testing para que el middleware aplique configuración adecuada.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemory"] = "true",
                ["InMemoryDbName"] = $"OCAP_Test_{Guid.NewGuid()}"
            });
        });
    }
}
