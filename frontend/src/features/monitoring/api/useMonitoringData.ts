import { useQuery } from "@tanstack/react-query";

export interface SystemMetricPoint {
  timestamp: string;
  cpuPercent: number;
  memoryMb: number;
  dbConnections: number;
  apiLatencyMs: number;
}

export interface AuditLogEntry {
  id: string;
  level: "info" | "warning" | "error";
  source: string;
  message: string;
  timestamp: string;
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

const MOCK_MONITORING_DATA: MonitoringData = {
  metrics: [
    { timestamp: "14:30", cpuPercent: 12, memoryMb: 512, dbConnections: 8, apiLatencyMs: 42 },
    { timestamp: "14:31", cpuPercent: 18, memoryMb: 524, dbConnections: 12, apiLatencyMs: 45 },
    { timestamp: "14:32", cpuPercent: 24, memoryMb: 540, dbConnections: 14, apiLatencyMs: 50 },
    { timestamp: "14:33", cpuPercent: 16, memoryMb: 530, dbConnections: 10, apiLatencyMs: 44 },
    { timestamp: "14:34", cpuPercent: 32, memoryMb: 580, dbConnections: 18, apiLatencyMs: 65 },
    { timestamp: "14:35", cpuPercent: 14, memoryMb: 535, dbConnections: 11, apiLatencyMs: 41 },
  ],
  logs: [
    { id: "log-1", level: "info", source: "OCAP.Agents.Runtime", message: "EnterpriseAssistantAgent inicializado correctamente en canal Telegram.", timestamp: "14:35:12" },
    { id: "log-2", level: "info", source: "OCAP.Security.Vault", message: "Claves API desencriptadas exitosamente para tenant 'OCAP Enterprise HQ'.", timestamp: "14:34:50" },
    { id: "log-3", level: "warning", source: "OCAP.Channels.WhatsApp", message: "Reintento de sincronización webhook tras pico de latencia (180ms).", timestamp: "14:32:05" },
    { id: "log-4", level: "error", source: "OCAP.Providers.Local", message: "Error al conectar con modelo local en localhost:11434. Failover activado a OpenAI.", timestamp: "14:28:10" },
  ],
  summary: {
    cpuAverage: 19.3,
    memoryPeakMb: 580,
    uptimePercent: 99.98,
    errorRate: "0.02%",
  },
};

export function useMonitoringData() {
  return useQuery<MonitoringData>({
    queryKey: ["monitoringData"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_MONITORING_DATA;
    },
    staleTime: 15000,
  });
}
