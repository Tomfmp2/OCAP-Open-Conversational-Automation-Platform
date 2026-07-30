# OpenTelemetry — estado de vulnerabilidades

OCAP usa el stack OpenTelemetry .NET **1.17.0** (exporter Prometheus AspNetCore `1.17.0-beta.1`, único exporter Prometheus alineado con 1.17).

## NU1902 / CVE-2026-42191

Las advertencias NU1902 sobre `OpenTelemetry.Exporter.OpenTelemetryProtocol` ≤ 1.15.2 (`GHSA-4625-4j76-fww9`, disk retry temp path) quedan **mitigadas** al actualizar a **1.17.0** (≥ 1.15.3).

Referencias:

- https://github.com/advisories/GHSA-4625-4j76-fww9
- https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol/1.17.0

## Configuración OCAP

- OTLP: `OpenTelemetry:OtlpEndpoint` (Compose → Jaeger `:4317`)
- Prometheus scrape: `GET /metrics`
- No se habilita `OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY=disk` en Compose.

Si NuGet vuelve a marcar vulnerabilidades en versiones futuras, documentar aquí el advisory y bloquear el bump hasta paquete estable.
