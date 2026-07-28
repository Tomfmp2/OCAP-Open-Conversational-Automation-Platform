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
}

const DEFAULT_MONITORING_DATA: MonitoringData = {
  metrics: [],
  logs: [],
  summary: { cpuAverage: 0, memoryPeakMb: 0, uptimePercent: 100, errorRate: "0%" },
};

export function useMonitoringData() {
  return useQuery<MonitoringData>({
    queryKey: ["monitoringData"],
    queryFn: async () => {
      try {
        if (typeof window === "undefined") return DEFAULT_MONITORING_DATA;
        const res = await fetch("/api/health");
        if (!res.ok) return DEFAULT_MONITORING_DATA;
        const data = await res.json();
        return {
          metrics: data?.metrics || [],
          logs: data?.logs || [],
          summary: data?.summary || DEFAULT_MONITORING_DATA.summary,
        };
      } catch {
        return DEFAULT_MONITORING_DATA;
      }
    },
    staleTime: 15000,
  });
}
