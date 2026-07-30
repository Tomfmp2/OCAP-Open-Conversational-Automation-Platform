using OCAP.Api.Extensions;
using OCAP.Api.Middlewares;
using OCAP.Application.Extensions;
using OCAP.Channels.Abstractions.Extensions;
using OCAP.Channels.WhatsApp.Extensions;
using OCAP.Channels.Telegram.Extensions;
using OCAP.Infrastructure.Extensions;
using OCAP.Knowledge.Infrastructure;
using Serilog;
using Serilog.Events;

// Bootstrap Logger for initial startup failures
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting OCAP API Server");

    var builder = WebApplication.CreateBuilder(args);

    // Replace built-in logging with Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());
// Limita el tamaño máximo del cuerpo de las peticiones para prevenir ataques de consumo de memoria.
// Se aplica globalmente antes de llegar a los controladores.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB máximo por petición para soporte de documentos
});

// Registra servicios de la capa de aplicación (casos de uso).
builder.Services.AddApplicationServices();

// Registra el módulo de Knowledge Base y RAG
builder.Services.AddKnowledgeModule(builder.Configuration);

// Registra servicios de infraestructura (EF Core, PostgreSQL, repositorios).
builder.Services.AddInfrastructure(builder.Configuration);

// Registra la arquitectura de canales y los canales nativos (WhatsApp, Telegram).
builder.Services.AddChannels(builder.Configuration);
builder.Services.AddWhatsAppChannel(builder.Configuration);
builder.Services.AddTelegramChannel(builder.Configuration);

// Registra servicios del gateway: controladores, Swagger, CORS, Rate Limiting, Seguridad.
builder.Services.AddApiServices(builder.Configuration);

// Compresión HTTP y response caching para APIs de lectura.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
builder.Services.AddResponseCaching();

var app = builder.Build();

// Aplica migraciones pendientes de EF Core antes de aceptar tráfico.
await app.Services.ApplyMigrationsAsync();

// Configuración del pipeline HTTP — el orden importa para la seguridad.

// El middleware de manejo de excepciones va primero para capturar errores de cualquier capa posterior.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Middleware de encabezados de seguridad (CSP, HSTS, X-Frame-Options, X-Content-Type-Options).
app.UseMiddleware<SecurityHeadersMiddleware>();

// HTTPS redirect solo en producción; en desarrollo se usa HTTP para facilitar el debugging.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Swagger solo en desarrollo para no exponer la documentación de la API en producción.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseResponseCaching();

// CORS debe configurarse antes del routing para que se aplique a todas las rutas.
app.UseCors("OcapCorsPolicy");

// Rate Limiting protege el gateway antes de que las peticiones lleguen a los controladores.
var rateLimitingSection = builder.Configuration.GetSection("RateLimiting");
var enableRateLimiting = rateLimitingSection.GetValue<bool>("EnableRateLimiting");
if (enableRateLimiting)
{
    app.UseRateLimiter();
}

app.UseRouting();

// Autenticación y autorización deben ejecutarse después del routing y antes de MapControllers.
app.UseAuthentication();
app.UseAuthorization();

// Serilog Request Logging middleware for structured HTTP logging
app.UseSerilogRequestLogging();

// Mapeo de controladores, SignalR hubs, health y métricas Prometheus.
app.MapControllers();
app.MapHub<OCAP.Api.Hubs.EventsHub>("/hubs/events");
app.MapOcapHealthEndpoints();
app.MapHealthChecks("/api/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready") || r.Tags.Contains("live") || r.Name == "Database" || r.Name == "postgres"
});

await app.RunAsync();

}
catch (Exception ex)
{
    Log.Fatal(ex, "OCAP API Server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Hace visible el tipo Program para la WebApplicationFactory de los tests de integración.
public partial class Program { }
