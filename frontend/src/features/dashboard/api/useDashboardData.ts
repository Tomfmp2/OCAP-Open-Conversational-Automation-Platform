import { useQuery } from "@tanstack/react-query";

export interface CostUsageDataPoint {
  date: string;
  openAiCost: number;
  geminiCost: number;
  claudeCost: number;
  localCost: number;
}

export interface ChannelThroughputDataPoint {
  hour: string;
  telegram: number;
  whatsApp: number;
  slack: number;
  web: number;
}

export interface ConversationSummary {
  id: string;
  user: string;
  senderName?: string;
  channel: string;
  lastMessage: string;
  timestamp: string;
  tokensUsed: number;
  status: string;
}

export interface DashboardDataDto {
  metrics: {
    totalExecutions: number;
    executionsChange: string;
    activeChannelsCount: number;
    totalChannelsCount: number;
    monthlyAiCostUsd: number;
    aiCostChange: string;
    systemHealthPercentage: number;
  };
  conversations: ConversationSummary[];
  costTrends: CostUsageDataPoint[];
  throughputTrends: ChannelThroughputDataPoint[];
  agentStatus: {
    name: string;
    status: string;
    activeProvider: string;
    memoryUsedMb: number;
    registeredTools: number;
  };
}

const DEFAULT_DASHBOARD_DATA: DashboardDataDto = {
  metrics: {
    totalExecutions: 0,
    executionsChange: "0%",
    activeChannelsCount: 0,
    totalChannelsCount: 0,
    monthlyAiCostUsd: 0,
    aiCostChange: "0%",
    systemHealthPercentage: 100,
  },
  conversations: [],
  costTrends: [],
  throughputTrends: [],
  agentStatus: {
    name: "EnterpriseAssistantAgent",
    status: "idle",
    activeProvider: "Sin Configurar",
    memoryUsedMb: 0,
    registeredTools: 0,
  },
};

export function useDashboardData() {
  return useQuery<DashboardDataDto>({
    queryKey: ["dashboardData"],
    queryFn: async () => {
      try {
        if (typeof window === "undefined") return DEFAULT_DASHBOARD_DATA;
        const res = await fetch("/api/dashboard/stats");
        if (!res.ok) return DEFAULT_DASHBOARD_DATA;
        const data = await res.json();
        return data || DEFAULT_DASHBOARD_DATA;
      } catch {
        return DEFAULT_DASHBOARD_DATA;
      }
    },
    staleTime: 15000,
  });
}
