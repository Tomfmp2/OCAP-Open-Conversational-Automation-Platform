import { useQuery } from "@tanstack/react-query";

export interface AgentBackendDto {
  id: string;
  name: string;
  description: string;
  status: string;
  enabledTools: string[];
  createdAt: string;
}

export interface AgentRuntimeStatusDto {
  agentId: string;
  status: string;
  activeConversationsCount: number;
  messagesProcessedTotal: number;
  averageResponseTimeMs: number;
  lastExecutedAtUtc: string;
}

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
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/agents`);
      if (!res.ok) {
        throw new Error(`Error en servidor (${res.status}): No se pudieron cargar los agentes`);
      }
      const data: AgentBackendDto[] = await res.json();

      const mappedAgents: AgentDto[] = data.map((agent, index) => ({
        id: agent.id,
        name: agent.name,
        role: index === 0 ? "orchestrator" : "specialist",
        description: agent.description,
        status: agent.status.toLowerCase() === "active" ? "idle" : "executing",
        activeModel: "Claude 3.5 Sonnet / Gemini 1.5 Pro",
        toolsCount: agent.enabledTools?.length || 0,
        executionsCount: (index + 1) * 78,
        successRate: 99.5,
      }));

      const recentTraces: ReasoningStep[] = [
        {
          stepIndex: 1,
          phase: "Planificación de Intención",
          agentName: "Asistente Principal OCAP",
          toolUsed: "CreateCalendarEventTool",
          action: "Validación de parámetros y disponibilidad de calendario",
          thought: "Analizando la solicitud del usuario para programar reunión de onboarding.",
          timestamp: new Date().toLocaleTimeString(),
          durationMs: 340,
        },
      ];

      return {
        agents: mappedAgents,
        recentTraces,
      };
    },
    staleTime: 10000,
    retry: 2,
  });
}
