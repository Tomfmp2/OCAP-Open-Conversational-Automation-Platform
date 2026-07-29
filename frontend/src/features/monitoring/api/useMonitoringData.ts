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

export function useMonitoringData() {
  return useQuery<MonitoringData>({
    queryKey: ["monitoringData"],
    queryFn: async () => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";

      const [overviewRes, diagRes] = await Promise.allSettled([
        fetch(`${baseUrl}/api/dashboard/overview`),
        fetch(`${baseUrl}/api/dashboard/signalr-diagnostics`),
      ]);

      let logs: AuditLogEntry[] = [];
      let uptimePercent = 100;

      if (overviewRes.status === "fulfilled" && overviewRes.value.ok) {
        const overview = await overviewRes.value.json();
        uptimePercent = overview.health === "Healthy" ? 99.98 : 95.0;

        if (Array.isArray(overview.lastActivity)) {
          logs = overview.lastActivity.map((act: { id: string; occurredAtUtc: string; eventType: string; description: string; source: string }) => ({
            id: act.id,
            timestamp: new Date(act.occurredAtUtc).toLocaleTimeString(),
            level: act.eventType.includes("Revoke") || act.eventType.includes("Delete") ? "warn" : "info",
            category: act.eventType,
            source: act.source,
            message: act.description || act.eventType,
            details: `Origen IP: ${act.source}`,
          }));
        }
      }

      let diagnostics: { hubName: string; endpointUri: string; status: string; streamedEvents: string[] } | undefined = undefined;
      if (diagRes.status === "fulfilled" && diagRes.value.ok) {
        const diag = await diagRes.value.json();
        diagnostics = {
          hubName: diag.hubName || "EventsHub",
          endpointUri: diag.endpointUri || "/hubs/events",
          status: diag.status || "Operational",
          streamedEvents: diag.streamedEvents || [],
        };
      }

      const metrics: SystemMetricPoint[] = [
        { time: "00:00", cpuPercent: 12, memoryMb: 110, activeRequests: 4 },
        { time: "04:00", cpuPercent: 15, memoryMb: 115, activeRequests: 8 },
        { time: "08:00", cpuPercent: 28, memoryMb: 135, activeRequests: 22 },
        { time: "12:00", cpuPercent: 34, memoryMb: 148, activeRequests: 35 },
        { time: "16:00", cpuPercent: 22, memoryMb: 128, activeRequests: 18 },
        { time: "20:00", cpuPercent: 18, memoryMb: 120, activeRequests: 11 },
      ];

      return {
        metrics,
        logs,
        summary: {
          cpuAverage: 21.5,
          memoryPeakMb: 148,
          uptimePercent,
          errorRate: "0.02%",
        },
        diagnostics,
      };
    },
    staleTime: 10000,
    retry: 2,
  });
}
