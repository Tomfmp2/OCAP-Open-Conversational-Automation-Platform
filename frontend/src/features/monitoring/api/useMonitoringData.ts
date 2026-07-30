import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

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
  };
  diagnostics?: {
    hubName: string;
    endpointUri: string;
    status: string;
    streamedEvents: string[];
  };
}

const metricsHistory: SystemMetricPoint[] = [];
const MAX_HISTORY_POINTS = 8;

export function useMonitoringData() {
  return useQuery<MonitoringData>({
    queryKey: ["monitoringData"],
    queryFn: async () => {
      const [overview, diag, sysMetric] = await Promise.allSettled([
        apiClient.get<{
          health: string;
          lastActivity?: Array<{
            id: string;
            occurredAtUtc: string;
            eventType: string;
            description: string;
            source: string;
          }>;
        }>("/api/dashboard/overview"),
        apiClient.get<{
          hubName?: string;
          endpointUri?: string;
          status?: string;
          streamedEvents?: string[];
        }>("/api/dashboard/signalr-diagnostics"),
        apiClient.get<{
          cpuPercent?: number;
          memoryMb?: number;
          activeThreads?: number;
        }>("/api/dashboard/system-metrics"),
      ]);

      let logs: AuditLogEntry[] = [];
      if (overview.status === "fulfilled") {
        if (Array.isArray(overview.value.lastActivity)) {
          logs = overview.value.lastActivity.map((act) => ({
            id: act.id,
            timestamp: new Date(act.occurredAtUtc).toLocaleTimeString(),
            level:
              act.eventType.includes("Revoke") || act.eventType.includes("Delete")
                ? "warn"
                : "info",
            category: act.eventType,
            source: act.source,
            message: act.description || act.eventType,
            details: `Origen IP: ${act.source}`,
          }));
        }
      }

      let diagnostics: MonitoringData["diagnostics"];
      if (diag.status === "fulfilled") {
        diagnostics = {
          hubName: diag.value.hubName || "EventsHub",
          endpointUri: diag.value.endpointUri || "/hubs/events",
          status: diag.value.status || "Operational",
          streamedEvents: diag.value.streamedEvents || [],
        };
      }

      let cpuPercent = 0;
      let memoryMb = 0;

      if (sysMetric.status === "fulfilled") {
        cpuPercent = sysMetric.value.cpuPercent ?? 0;
        memoryMb = sysMetric.value.memoryMb ?? 0;

        const point: SystemMetricPoint = {
          time: new Date().toLocaleTimeString("es-MX", {
            hour: "2-digit",
            minute: "2-digit",
          }),
          cpuPercent,
          memoryMb,
          activeRequests: sysMetric.value.activeThreads ?? 0,
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
        },
        diagnostics,
      };
    },
    staleTime: 10000,
    refetchInterval: 15000,
    retry: 2,
  });
}
