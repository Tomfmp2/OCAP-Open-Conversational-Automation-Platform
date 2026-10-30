import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface Agent {
  id: string;
  name: string;
  description: string;
  status: string;
  systemPrompt: string;
  enabledTools: string[];
  /** Colecciones de conocimiento que consulta; vacía, todas. */
  knowledgeBaseIds: string[];
  /** Recuerda a los contactos entre conversaciones. */
  rememberContacts: boolean;
  createdAt: string;
}

export interface AgentTool {
  name: string;
  description: string;
}

export interface AgentDraft {
  name: string;
  description: string;
  systemPrompt: string;
  allowedTools: string[];
  knowledgeBaseIds: string[];
  rememberContacts: boolean;
}

export interface AgentTestTurn {
  fromContact: boolean;
  text: string;
}

export interface AgentTestResult {
  reply: string;
  providerUsed: string;
  agentName: string;
  transferToHuman: boolean;
}

/** Id con el que la API prueba el agente de atención por defecto (el que responde en canales sin agente). */
export const DEFAULT_AGENT_ID = "00000000-0000-0000-0000-000000000000";

const AGENTS_KEY = ["agentsData"];

export function useAgents() {
  return useQuery<Agent[]>({
    queryKey: AGENTS_KEY,
    queryFn: () => apiClient.get<Agent[]>("/api/agents"),
    staleTime: 10_000,
  });
}

export function useAgentTools() {
  return useQuery<AgentTool[]>({
    queryKey: ["agentTools"],
    queryFn: () => apiClient.get<AgentTool[]>("/api/agents/tools"),
    staleTime: 5 * 60_000,
  });
}

/** Crea el agente, o lo actualiza si se pasa su id. */
export function useSaveAgent() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, draft }: { id?: string; draft: AgentDraft }) =>
      id ? apiClient.put(`/api/agents/${id}`, draft) : apiClient.post("/api/agents", draft),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: AGENTS_KEY }),
  });
}

export function useDeleteAgent() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/agents/${id}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: AGENTS_KEY });
      void queryClient.invalidateQueries({ queryKey: ["channelsData"] });
    },
  });
}

/** Prueba un agente con el mismo motor que los canales; no guarda nada ni ejecuta herramientas reales. */
export function testAgent(agentId: string, message: string, history: AgentTestTurn[]) {
  return apiClient.post<AgentTestResult>(`/api/agents/${agentId}/test`, { message, history }, { timeout: 120_000 });
}
