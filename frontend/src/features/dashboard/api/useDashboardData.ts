import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

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
      const [overview, metricsBackend, toolsList, convosJson] = await Promise.all([
        apiClient.get<DashboardOverviewDto>("/api/dashboard/overview"),
        apiClient.get<{
          averageResponseTimeMs: number;
          successRatePercentage: number;
          activeConversationsToday: number;
          messagesProcessedToday: number;
        }>("/api/dashboard/metrics").catch(() => ({
          averageResponseTimeMs: 0,
          successRatePercentage: 100,
          activeConversationsToday: 0,
          messagesProcessedToday: 0,
        })),
        apiClient.get<unknown[]>("/api/tools").catch(() => []),
        apiClient
          .get<{ data?: { items?: unknown[] }; items?: unknown[] }>(
            "/api/conversations?pageSize=5"
          )
          .catch(() => ({ data: { items: [] as unknown[] } })),
      ]);

      const toolCount = Array.isArray(toolsList) ? toolsList.length : 0;
      const convoPayload = convosJson as { data?: { items?: unknown[] }; items?: unknown[] };
      const items = convoPayload?.data?.items ?? convoPayload?.items ?? [];

      const conversationsList: ConversationSummary[] = Array.isArray(items)
        ? (items as Array<{
            id: string;
            userId: string;
            channel: string;
            status: string;
            updatedAt: string;
            messageCount: number;
            lastMessageSnippet?: string;
          }>).map((c) => ({
              id: c.id,
              user: c.userId || "Usuario",
              senderName: c.userId ? `User-${c.userId.substring(0, 6)}` : "Cliente",
              channel: c.channel || "Web",
              lastMessage: c.lastMessageSnippet || "Sin mensajes",
              timestamp: c.updatedAt ? new Date(c.updatedAt).toLocaleTimeString() : "Hoy",
              tokensUsed: c.messageCount || 0,
              status: c.status || "Active",
            }))
        : [];

      const systemHealth =
        metricsBackend.successRatePercentage > 0
          ? metricsBackend.successRatePercentage
          : overview.health === "Healthy"
            ? 100
            : 85;

      return {
        metrics: {
          totalExecutions: overview.workflows?.executionsToday || 0,
          executionsChange:
            metricsBackend.messagesProcessedToday > 0
              ? `+${metricsBackend.messagesProcessedToday} msgs`
              : "0%",
          activeChannelsCount: overview.channels?.connectedCount || 0,
          totalChannelsCount: overview.channels?.totalCount || 0,
          monthlyAiCostUsd: 0,
          aiCostChange: "0%",
          systemHealthPercentage: Math.round(systemHealth),
        },
        overview,
        conversations: conversationsList,
        costTrends: [],
        throughputTrends: [],
        agentStatus: {
          name:
            overview.agents?.totalCount > 0
              ? `${overview.agents.activeCount} Agente(s) Activo(s)`
              : "Sin Agentes Registrados",
          status: overview.agents?.runtimeStatus || "Operational",
          activeProvider: "—",
          memoryUsedMb: 0,
          registeredTools: toolCount,
        },
      };
    },
    staleTime: 10000,
    retry: 2,
  });
}
