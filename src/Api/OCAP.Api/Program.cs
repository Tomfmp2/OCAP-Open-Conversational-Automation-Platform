using OCAP.Api.Extensions;
using OCAP.Api.Middlewares;
using OCAP.Application.Extensions;
using OCAP.Channels.Abstractions.Extensions;
using OCAP.Channels.WhatsApp.Extensions;
using OCAP.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Limita el tamaño máximo del cuerpo de las peticiones para prevenir ataques de consumo de memoria.
// Se aplica globalmente antes de llegar a los controladores.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1 * 1024 * 1024; // 1 MB máximo por petición.
});

// Registra servicios de la capa de aplicación (casos de uso).
builder.Services.AddApplicationServices();

// Registra servicios de infraestructura (EF Core, PostgreSQL, repositorios).
builder.Services.AddInfrastructure(builder.Configuration);

// Registra la arquitectura de canales y el canal de WhatsApp Evolution API.
builder.Services.AddChannels(builder.Configuration);
builder.Services.AddWhatsAppChannel(builder.Configuration);

// Registra servicios del gateway: controladores, Swagger, CORS, Rate Limiting.
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Configuración del pipeline HTTP — el orden importa para la seguridad.

// El middleware de manejo de excepciones va primero para capturar errores de cualquier capa posterior.
app.UseMiddleware<ExceptionHandlingMiddleware>();

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
// Solo activo cuando el Rate Limiter está registrado (determinado por la configuración).
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
// Sin esta declaración, el proyecto de test no puede referenciar la clase Program interna.
public partial class Program { }
