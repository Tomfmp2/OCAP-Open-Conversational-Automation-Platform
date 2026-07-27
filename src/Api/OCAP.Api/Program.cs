using OCAP.Api.Extensions;
using OCAP.Api.Middlewares;
using OCAP.Application.Extensions;
using OCAP.Channels.Abstractions.Extensions;
using OCAP.Channels.WhatsApp.Extensions;
using OCAP.Infrastructure.Extensions;
using OCAP.Knowledge.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Limita el tamaño máximo del cuerpo de las peticiones para prevenir ataques de consumo de memoria.
// Se aplica globalmente antes de llegar a los controladores.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB máximo por petición para soporte de documentos
});

// Registra servicios de la capa de aplicación (casos de uso).
builder.Services.AddApplicationServices();

// Registra el módulo de Knowledge Base y RAG
builder.Services.AddKnowledgeModule();

// Registra servicios de infraestructura (EF Core, PostgreSQL, repositorios).
builder.Services.AddInfrastructure(builder.Configuration);

// Registra la arquitectura de canales y el canal de WhatsApp Evolution API.
builder.Services.AddChannels(builder.Configuration);
builder.Services.AddWhatsAppChannel(builder.Configuration);

// Registra servicios del gateway: controladores, Swagger, CORS, Rate Limiting, Seguridad.
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

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

// Mapeo de controladores y health checks.
app.MapControllers();
app.MapHealthChecks("/api/health");

await app.RunAsync();

// Hace visible el tipo Program para la WebApplicationFactory de los tests de integración.
public partial class Program { }
