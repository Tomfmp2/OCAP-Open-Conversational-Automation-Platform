import { useQuery } from "@tanstack/react-query";

export interface DashboardMetrics {
  totalExecutions: number;
  executionsChange: string;
  activeChannelsCount: number;
  totalChannelsCount: number;
  monthlyAiCostUsd: number;
  aiCostChange: string;
  systemHealthPercentage: number;
}

export interface ConversationSummary {
  id: string;
  channel: "Telegram" | "WhatsApp" | "Google" | "Slack";
  senderName: string;
  lastMessage: string;
  timestamp: string;
  unreadCount: number;
  status: "active" | "resolved" | "pending";
}

export interface CostUsageDataPoint {
  date: string;
  tokens: number;
  costUsd: number;
}

export interface ChannelThroughputDataPoint {
  time: string;
  telegram: number;
  whatsapp: number;
  google: number;
}

export interface DashboardData {
  metrics: DashboardMetrics;
  conversations: ConversationSummary[];
  costTrends: CostUsageDataPoint[];
  throughputTrends: ChannelThroughputDataPoint[];
  agentStatus: {
    name: string;
    status: "idle" | "busy" | "error";
    activeProvider: string;
    memoryUsedMb: number;
    registeredTools: number;
  };
}

const MOCK_DASHBOARD_DATA: DashboardData = {
  metrics: {
    totalExecutions: 14280,
    executionsChange: "+18.4%",
    activeChannelsCount: 4,
    totalChannelsCount: 4,
    monthlyAiCostUsd: 42.85,
    aiCostChange: "-8.2%",
    systemHealthPercentage: 99.8,
  },
  conversations: [
    {
      id: "conv-1",
      channel: "Telegram",
      senderName: "Carlos Mendoza (Cliente)",
      lastMessage: "¿Podrían confirmar la fecha de entrega del pedido #9402?",
      timestamp: "Hace 3 min",
      unreadCount: 1,
      status: "active",
    },
    {
      id: "conv-2",
      channel: "WhatsApp",
      senderName: "Soporte Técnico Enterprise",
      lastMessage: "El ticket #402 fue resuelto automáticamente por el agente.",
      timestamp: "Hace 12 min",
      unreadCount: 0,
      status: "resolved",
    },
    {
      id: "conv-3",
      channel: "Google",
      senderName: "Notificación de Calendario",
      lastMessage: "Reunión de Sincronización programada para mañana 10:00 AM.",
      timestamp: "Hace 45 min",
      unreadCount: 0,
      status: "pending",
    },
  ],
  costTrends: [
    { date: "Jul 22", tokens: 120000, costUsd: 1.2 },
    { date: "Jul 23", tokens: 250000, costUsd: 2.5 },
    { date: "Jul 24", tokens: 180000, costUsd: 1.8 },
    { date: "Jul 25", tokens: 340000, costUsd: 3.4 },
    { date: "Jul 26", tokens: 290000, costUsd: 2.9 },
    { date: "Jul 27", tokens: 410000, costUsd: 4.1 },
    { date: "Jul 28", tokens: 480000, costUsd: 4.8 },
  ],
  throughputTrends: [
    { time: "08:00", telegram: 45, whatsapp: 30, google: 12 },
    { time: "10:00", telegram: 120, whatsapp: 95, google: 40 },
    { time: "12:00", telegram: 210, whatsapp: 180, google: 65 },
    { time: "14:00", telegram: 190, whatsapp: 160, google: 50 },
    { time: "16:00", telegram: 250, whatsapp: 220, google: 85 },
    { time: "18:00", telegram: 140, whatsapp: 110, google: 30 },
  ],
  agentStatus: {
    name: "Enterprise Assistant Agent",
    status: "idle",
    activeProvider: "OpenAI (gpt-4o)",
    memoryUsedMb: 128,
    registeredTools: 14,
  },
};

export function useDashboardData() {
  return useQuery<DashboardData>({
    queryKey: ["dashboardData"],
    queryFn: async () => {
      // Simulate real API latency
      await new Promise((res) => setTimeout(res, 400));
      return MOCK_DASHBOARD_DATA;
    },
    staleTime: 30000,
  });
}
