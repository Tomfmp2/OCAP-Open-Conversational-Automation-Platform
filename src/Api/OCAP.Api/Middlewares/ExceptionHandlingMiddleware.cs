using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace OCAP.Api.Middlewares;

// Middleware global que intercepta cualquier excepción no manejada en el pipeline HTTP.
// Garantiza que nunca se expongan stack traces, rutas internas ni información del servidor.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    // Ejecuta el siguiente middleware y captura cualquier excepción no controlada.
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            // Las excepciones de argumento indican peticiones malformadas (400).
            _logger.LogWarning(ex, "Petición inválida rechazada: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Errores de lógica de negocio que indican estados inválidos (422).
            _logger.LogWarning(ex, "Operación inválida en: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.UnprocessableEntity, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            // Recursos no encontrados (404).
            _logger.LogInformation(ex, "Recurso no encontrado en: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex, HttpStatusCode.NotFound, "El recurso solicitado no existe.");
        }
        catch (Exception ex)
        {
            // Cualquier excepción no controlada se trata como error 500.
            // Nunca se expone el detalle interno al cliente en producción.
            _logger.LogError(ex, "Error interno no controlado en: {Path}", context.Request.Path);
            var message = _env.IsDevelopment()
                ? ex.Message
                : "Se ha producido un error interno en el servidor.";
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError, message);
        }
    }

    // Formatea la respuesta de error con el estándar ApiResponse de OCAP.
    // Nunca incluye stack traces en la respuesta al cliente.
    private static Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode,
        string clientMessage)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = GetTitle(statusCode),
            Detail = clientMessage,
            Instance = context.Request.Path
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);
        return context.Response.WriteAsync(json);
    }

    // Determina el título del problema según el código HTTP para mantener consistencia RFC 9457.
    private static string GetTitle(HttpStatusCode code) => code switch
    {
        HttpStatusCode.BadRequest => "Solicitud inválida",
        HttpStatusCode.NotFound => "Recurso no encontrado",
        HttpStatusCode.UnprocessableEntity => "Error de validación de negocio",
        _ => "Error interno del servidor"
    };
}
