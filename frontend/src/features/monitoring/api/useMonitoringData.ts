import { useQuery } from "@tanstack/react-query";

export interface SystemMetricPoint {
  time: string;
  cpuPercent: number;
  memoryMb: number;
  activeRequests: number;
}

export interface AuditLogEntry {
  id: string;
  timestamp: string;
  level: "info" | "warn" | "warning" | "error" | "security";
  category: string;
  source?: string;
  message: string;
  details?: string;
}

export interface MonitoringData {
  metrics: SystemMetricPoint[];
  logs: AuditLogEntry[];
  summary: {
    cpuAverage: number;
    memoryPeakMb: number;
    uptimePercent: number;
    errorRate: string;
  };
  diagnostics?: {
    hubName: string;
    endpointUri: string;
    status: string;
    streamedEvents: string[];
  };
}

// Historial de puntos de métricas acumulados en memoria para el gráfico
const metricsHistory: SystemMetricPoint[] = [];
const MAX_HISTORY_POINTS = 8;

export function useMonitoringData() {
  return useQuery<MonitoringData>({
    queryKey: ["monitoringData"],
    queryFn: async () => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";

      // Llamadas paralelas al backend real
      const [overviewRes, diagRes, metricsRes] = await Promise.allSettled([
        fetch(`${baseUrl}/api/dashboard/overview`),
        fetch(`${baseUrl}/api/dashboard/signalr-diagnostics`),
        fetch(`${baseUrl}/api/dashboard/system-metrics`),
      ]);

      let logs: AuditLogEntry[] = [];
      let uptimePercent = 100;

      if (overviewRes.status === "fulfilled" && overviewRes.value.ok) {
        const overview = await overviewRes.value.json();
        uptimePercent = overview.health === "Healthy" ? 99.98 : 95.0;

        if (Array.isArray(overview.lastActivity)) {
          logs = overview.lastActivity.map(
            (act: {
              id: string;
              occurredAtUtc: string;
              eventType: string;
              description: string;
              source: string;
            }) => ({
              id: act.id,
              timestamp: new Date(act.occurredAtUtc).toLocaleTimeString(),
              level:
                act.eventType.includes("Revoke") ||
                act.eventType.includes("Delete")
                  ? "warn"
                  : "info",
              category: act.eventType,
              source: act.source,
              message: act.description || act.eventType,
              details: `Origen IP: ${act.source}`,
            })
          );
        }
      }

      let diagnostics:
        | {
            hubName: string;
            endpointUri: string;
            status: string;
            streamedEvents: string[];
          }
        | undefined = undefined;

      if (diagRes.status === "fulfilled" && diagRes.value.ok) {
        const diag = await diagRes.value.json();
        diagnostics = {
          hubName: diag.hubName || "EventsHub",
          endpointUri: diag.endpointUri || "/hubs/events",
          status: diag.status || "Operational",
          streamedEvents: diag.streamedEvents || [],
        };
      }

      // Métricas de sistema reales desde el proceso .NET
      let cpuPercent = 0;
      let memoryMb = 0;

      if (metricsRes.status === "fulfilled" && metricsRes.value.ok) {
        const sysMetric = await metricsRes.value.json();
        cpuPercent = sysMetric.cpuPercent ?? 0;
        memoryMb = sysMetric.memoryMb ?? 0;

        // Acumular en historial para el gráfico de series de tiempo
        const point: SystemMetricPoint = {
          time: new Date().toLocaleTimeString("es-MX", {
            hour: "2-digit",
            minute: "2-digit",
          }),
          cpuPercent,
          memoryMb,
          activeRequests: sysMetric.activeThreads ?? 0,
        };

        metricsHistory.push(point);
        if (metricsHistory.length > MAX_HISTORY_POINTS) {
          metricsHistory.shift();
        }
      }

      const metrics: SystemMetricPoint[] =
        metricsHistory.length > 0
          ? [...metricsHistory]
          : [
              // Fallback inicial con un punto real mientras se acumula historial
              {
                time: new Date().toLocaleTimeString("es-MX", {
                  hour: "2-digit",
                  minute: "2-digit",
                }),
                cpuPercent,
                memoryMb,
                activeRequests: 0,
              },
            ];

      const cpuAverage =
        metrics.length > 0
          ? metrics.reduce((s, p) => s + p.cpuPercent, 0) / metrics.length
          : 0;

      const memoryPeak = Math.max(...metrics.map((p) => p.memoryMb), 0);

      return {
        metrics,
        logs,
        summary: {
          cpuAverage: Math.round(cpuAverage * 10) / 10,
          memoryPeakMb: Math.round(memoryPeak * 10) / 10,
          uptimePercent,
          errorRate: "0.00%", // Se calculará en el futuro desde audit logs del backend
        },
        diagnostics,
      };
    },
    staleTime: 10000,
    refetchInterval: 15000, // Actualiza cada 15s para el gráfico de métricas
    retry: 2,
  });
}
