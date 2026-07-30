using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OCAP.Core.Events.Distributed;
using OCAP.Core.Storage;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Infrastructure.Telemetry;

namespace OCAP.Infrastructure.Extensions;

public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddOcapObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? OcapTelemetry.ServiceName;

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName, serviceVersion: OcapTelemetry.ServiceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(OcapTelemetry.ActivitySource.Name)
                    .AddSource("OCAP.Knowledge")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(OcapTelemetry.Meter.Name)
                    .AddMeter("OCAP.Knowledge.Metrics")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }

    public static IServiceCollection AddOcapHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var health = services.AddHealthChecks()
            .AddDbContextCheck<OCAPDbContext>("postgres", tags: new[] { "ready", "db" })
            .AddCheck<EventBusHealthCheck>("eventbus", tags: new[] { "ready", "messaging" })
            .AddCheck("self", () => HealthCheckResult.Healthy("process"), tags: new[] { "live" })
            .AddCheck("startup", () => HealthCheckResult.Healthy("started"), tags: new[] { "startup" });

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            health.AddRedis(redis, name: "redis", tags: new[] { "ready", "cache" });
        }

        health.AddCheck<ObjectStorageHealthCheck>("storage", tags: new[] { "ready", "storage" });
        health.AddCheck<TelemetryHealthCheck>("telemetry", tags: new[] { "ready", "telemetry" });

        return services;
    }

    public static WebApplication MapOcapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        }).AllowAnonymous();
        app.MapHealthChecks("/health/startup", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("startup")
        }).AllowAnonymous();
        app.MapHealthChecks("/api/health/system", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    totalDurationMs = report.TotalDuration.TotalMilliseconds,
                    entries = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        durationMs = e.Value.Duration.TotalMilliseconds
                    })
                });
            }
        }).AllowAnonymous();

        app.MapPrometheusScrapingEndpoint("/metrics").AllowAnonymous();
        return app;
    }
}

public sealed class EventBusHealthCheck : IHealthCheck
{
    private readonly IEventTransport _transport;

    public EventBusHealthCheck(IEventTransport transport) => _transport = transport;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var ok = await _transport.HealthCheckAsync(cancellationToken);
        return ok
            ? HealthCheckResult.Healthy($"Event transport {_transport.ProviderName} OK")
            : HealthCheckResult.Unhealthy($"Event transport {_transport.ProviderName} unavailable");
    }
}

public sealed class ObjectStorageHealthCheck : IHealthCheck
{
    private readonly IObjectStorage? _storage;

    public ObjectStorageHealthCheck(IServiceProvider sp)
    {
        _storage = sp.GetService<IObjectStorage>();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_storage is null)
        {
            return HealthCheckResult.Degraded("No object storage registered");
        }

        var ok = await _storage.HealthAsync(cancellationToken);
        return ok
            ? HealthCheckResult.Healthy(_storage.ProviderName)
            : HealthCheckResult.Unhealthy($"{_storage.ProviderName} unhealthy");
    }
}

public sealed class TelemetryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy($"ActivitySource={OcapTelemetry.ActivitySource.Name}; Meter={OcapTelemetry.Meter.Name}"));
    }
}
