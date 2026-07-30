using System.Diagnostics;

namespace OCAP.Api.Middlewares;

/// <summary>
/// Propaga X-Correlation-Id / X-Request-Id al inicio del pipeline y Activity Baggage.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[ExceptionHandlingMiddleware.CorrelationHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");
        var requestId = context.Request.Headers[ExceptionHandlingMiddleware.RequestIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Items[ExceptionHandlingMiddleware.CorrelationHeader] = correlationId;
        context.Items[ExceptionHandlingMiddleware.RequestIdHeader] = requestId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ExceptionHandlingMiddleware.CorrelationHeader] = correlationId;
            context.Response.Headers[ExceptionHandlingMiddleware.RequestIdHeader] = requestId;
            return Task.CompletedTask;
        });

        Activity.Current?.SetTag("ocap.correlation_id", correlationId);
        Activity.Current?.SetTag("ocap.request_id", requestId);

        await _next(context);
    }
}
