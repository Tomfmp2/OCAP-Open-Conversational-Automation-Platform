using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace OCAP.Api.Middlewares;

/// <summary>
/// Middleware global RFC 7807 ProblemDetails. No expone stack traces en producción.
/// </summary>
public class ExceptionHandlingMiddleware
{
    public const string CorrelationHeader = "X-Correlation-Id";
    public const string RequestIdHeader = "X-Request-Id";

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

    public async Task InvokeAsync(HttpContext context)
    {
        EnsureCorrelation(context);

        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validación fallida en {Path} corr={CorrelationId}",
                context.Request.Path, context.TraceIdentifier);
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Validación fallida",
                "Uno o más campos no son válidos.",
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso denegado en {Path}", context.Request.Path);
            await WriteProblemAsync(context, HttpStatusCode.Forbidden, "Acceso denegado",
                "No tiene permisos para realizar esta operación.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Petición inválida: {Path}", context.Request.Path);
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Solicitud inválida", ex.Message);
        }
        catch (KeyNotFoundException)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "Recurso no encontrado",
                "El recurso solicitado no existe.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida en {Path}", context.Request.Path);
            await WriteProblemAsync(context, HttpStatusCode.UnprocessableEntity, "Error de negocio", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno en {Path} corr={CorrelationId}",
                context.Request.Path, context.Items[CorrelationHeader]);
            var detail = _env.IsDevelopment()
                ? ex.Message
                : "Se ha producido un error interno en el servidor.";
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Error interno del servidor", detail);
        }
    }

    private static void EnsureCorrelation(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString("N");
        var requestId = context.Request.Headers[RequestIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Items[CorrelationHeader] = correlationId;
        context.Items[RequestIdHeader] = requestId;
        context.Response.Headers[CorrelationHeader] = correlationId;
        context.Response.Headers[RequestIdHeader] = requestId;
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail,
        IDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{(int)statusCode}"
        };

        problem.Extensions["correlationId"] = context.Items[CorrelationHeader];
        problem.Extensions["requestId"] = context.Items[RequestIdHeader];
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}
