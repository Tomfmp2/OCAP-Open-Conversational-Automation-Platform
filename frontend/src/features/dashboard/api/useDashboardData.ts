import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

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

export interface UserOverviewSummaryDto {
 totalCount: number;
 activeCount: number;
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
 users: UserOverviewSummaryDto;
 lastActivity: LastActivityDto[];
}

export interface DashboardDataDto {
 overview: DashboardOverviewDto;
}

export function useDashboardData() {
 return useQuery<DashboardDataDto>({
 queryKey: ["dashboardOverview"],
 queryFn: async () => {
 const overview = await apiClient.get<DashboardOverviewDto>(
 "/api/dashboard/overview"
 );
 return { overview };
 },
 staleTime: 10000,
 retry: 2,
 });
}
