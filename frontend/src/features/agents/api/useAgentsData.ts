import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface AgentBackendDto {
  id: string;
  name: string;
  description: string;
  status: string;
  enabledTools: string[];
  createdAt: string;
}

export interface AgentDto {
  id: string;
  name: string;
  role: "registered";
  description: string;
  status: "idle" | "thinking" | "executing" | "error";
  activeModel: string;
  toolsCount: number;
  executionsCount: number;
  successRate: number;
}

export type AgentInfo = AgentDto;

export interface ReasoningStep {
  id?: string;
  stepIndex: number;
  phase: string;
  agentName?: string;
  toolUsed?: string;
  action?: string;
  thought: string;
  timestamp: string;
  durationMs: number;
}

export interface AgentsData {
  agents: AgentDto[];
  recentTraces: ReasoningStep[];
}

export function useAgentsData() {
  return useQuery<AgentsData>({
    queryKey: ["agentsData"],
    queryFn: async () => {
      const data = await apiClient.get<AgentBackendDto[]>("/api/agents");

      const mappedAgents: AgentDto[] = data.map((agent) => ({
        id: agent.id,
        name: agent.name,
        role: "registered",
        description: agent.description,
        status:
          agent.status.toLowerCase() === "error"
            ? "error"
            : agent.status.toLowerCase() === "executing"
              ? "executing"
              : "idle",
        activeModel: "N/D",
        toolsCount: agent.enabledTools?.length || 0,
        executionsCount: 0,
        successRate: 0,
      }));

      return {
        agents: mappedAgents,
        recentTraces: [],
      };
    },
    staleTime: 10000,
    retry: 2,
  });
}

export function useCreateAgentMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: {
      name: string;
      description: string;
      systemPrompt: string;
      allowedTools?: string[];
    }) => {
      return apiClient.post("/api/agents", payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["agentsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });
}
