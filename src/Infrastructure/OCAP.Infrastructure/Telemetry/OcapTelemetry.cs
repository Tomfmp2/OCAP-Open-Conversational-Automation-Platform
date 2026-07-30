using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OCAP.Infrastructure.Telemetry;

public static class OcapTelemetry
{
    public const string ServiceName = "OCAP.Api";
    public const string ServiceVersion = "1.0.0";

    public static readonly ActivitySource ActivitySource = new("OCAP.Runtime", ServiceVersion);
    public static readonly Meter Meter = new("OCAP.Runtime.Metrics", ServiceVersion);

    public static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>("ocap_requests_total");
    public static readonly Counter<long> ErrorsTotal = Meter.CreateCounter<long>("ocap_errors_total");
    public static readonly Counter<long> RetriesTotal = Meter.CreateCounter<long>("ocap_retries_total");
    public static readonly Counter<long> EventPublishTotal = Meter.CreateCounter<long>("ocap_events_published_total");
    public static readonly Counter<long> EventConsumeTotal = Meter.CreateCounter<long>("ocap_events_consumed_total");
    public static readonly Counter<long> OutboxDispatchedTotal = Meter.CreateCounter<long>("ocap_outbox_dispatched_total");
    public static readonly Counter<long> AiTokensTotal = Meter.CreateCounter<long>("ocap_ai_tokens_total");
    public static readonly Histogram<double> RequestLatencyMs = Meter.CreateHistogram<double>("ocap_request_latency_ms");
    public static readonly Histogram<double> EventLatencyMs = Meter.CreateHistogram<double>("ocap_event_latency_ms");

    public static readonly ObservableGauge<long> GcHeapBytes = Meter.CreateObservableGauge(
        "ocap_gc_heap_bytes",
        () => GC.GetTotalMemory(false));

    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        => ActivitySource.StartActivity(name, kind);
}
