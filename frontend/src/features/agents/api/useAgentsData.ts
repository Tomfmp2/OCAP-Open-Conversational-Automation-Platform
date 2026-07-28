import { useQuery } from "@tanstack/react-query";

export interface AgentDto {
  id: string;
  name: string;
  role: "orchestrator" | "specialist" | "subagent";
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
      try {
        if (typeof window === "undefined") return { agents: [], recentTraces: [] };
        const res = await fetch("/api/agents");
        if (!res.ok) return { agents: [], recentTraces: [] };
        const data = await res.json();
        return {
          agents: Array.isArray(data) ? data : data?.agents || [],
          recentTraces: data?.recentTraces || [],
        };
      } catch {
        return { agents: [], recentTraces: [] };
      }
    },
    staleTime: 15000,
  });
}
