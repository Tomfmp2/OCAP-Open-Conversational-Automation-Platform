# Observabilidad (Sprint 4)

## Implementado

- `ActivitySource` / `Meter`: `OCAP.Runtime` (`OcapTelemetry`) + Knowledge telemetry.
- OpenTelemetry ASP.NET Core + HttpClient instrumentation.
- Exporters: OTLP (Jaeger collector vía `OpenTelemetry:OtlpEndpoint`), Prometheus scrape en `/metrics`.
- Health:
  - `/health/live` — liveness
  - `/health/ready` — readiness (postgres, eventbus, storage, telemetry)
  - `/health/startup` — startup
  - `/api/health` — alias ready/live
  - `/api/health/diagnostic` — detalle instalador
  - `/api/health/system` — JSON detallado

## Compose

- `jaeger` (UI `:16686`, OTLP `:4317/:4318`)
- `prometheus` (`:9090`) scrape `ocap-api:5000/metrics`

## No incluido en este repositorio

- Grafana dashboards, Loki, Zipkin server dedicado (usar OTLP → collector si se requiere Zipkin).
- Agents de APM comerciales.
