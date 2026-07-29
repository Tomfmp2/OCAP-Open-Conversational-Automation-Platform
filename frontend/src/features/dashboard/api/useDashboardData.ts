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

export interface ServerUptimeDto {
  startedAtUtc: string;
  uptimeSeconds: number;
  uptimeFormatted: string;
}

export interface WorkflowOverviewSummaryDto {
  totalCount: number;
  activeCount: number;
  failedCount: number;
  executionsToday: number;
}

export interface AgentOverviewSummaryDto {
  totalCount: number;
  activeCount: number;
  runtimeStatus: string;
}

export interface ChannelOverviewSummaryDto {
  totalCount: number;
  connectedCount: number;
  telegramConnected: boolean;
  whatsappConnected: boolean;
}

export interface TenantOverviewSummaryDto {
  totalCount: number;
  activeCount: number;
}

export interface UserOverviewSummaryDto {
  totalCount: number;
  activeCount: number;
}

export interface ApiKeyOverviewSummaryDto {
  totalCount: number;
  activeCount: number;
  revokedCount: number;
}

export interface WebhookOverviewSummaryDto {
  totalSubscriptions: number;
  activeSubscriptions: number;
  deliveriesToday: number;
  failedDeliveriesToday: number;
}

export interface LastActivityDto {
  id: string;
  eventType: string;
  description: string;
  source: string;
  occurredAtUtc: string;
  tenantId: string;
}

export interface DashboardOverviewDto {
  health: string;
  uptime: ServerUptimeDto;
  workflows: WorkflowOverviewSummaryDto;
  agents: AgentOverviewSummaryDto;
  channels: ChannelOverviewSummaryDto;
  tenants: TenantOverviewSummaryDto;
  users: UserOverviewSummaryDto;
  apiKeys: ApiKeyOverviewSummaryDto;
  webhooks: WebhookOverviewSummaryDto;
  lastActivity: LastActivityDto[];
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
  overview: DashboardOverviewDto;
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

export function useDashboardData() {
  return useQuery<DashboardDataDto>({
    queryKey: ["dashboardOverview"],
    queryFn: async () => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/dashboard/overview`);
      if (!res.ok) {
        throw new Error(`Error en servidor (${res.status}): No se pudo cargar el estado del dashboard`);
      }
      const overview: DashboardOverviewDto = await res.json();

      return {
        metrics: {
          totalExecutions: overview.workflows?.executionsToday || 0,
          executionsChange: "+12%",
          activeChannelsCount: overview.channels?.connectedCount || 0,
          totalChannelsCount: overview.channels?.totalCount || 0,
          monthlyAiCostUsd: 14.5,
          aiCostChange: "-3%",
          systemHealthPercentage: overview.health === "Healthy" ? 100 : 85,
        },
        overview,
        conversations: [],
        costTrends: [],
        throughputTrends: [],
        agentStatus: {
          name: "Asistente Principal OCAP",
          status: overview.agents?.runtimeStatus || "Operational",
          activeProvider: "OpenAI & Gemini",
          memoryUsedMb: 128,
          registeredTools: 5,
        },
      };
    },
    staleTime: 10000,
    retry: 2,
  });
}
