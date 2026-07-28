import { useQuery } from "@tanstack/react-query";

export interface AgentInfo {
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

export interface ReasoningStep {
  id: string;
  agentName: string;
  action: string;
  thought: string;
  toolUsed?: string;
  status: "success" | "pending" | "failed";
  timestamp: string;
}

export interface AgentsData {
  agents: AgentInfo[];
  recentTraces: ReasoningStep[];
}

const MOCK_AGENTS_DATA: AgentsData = {
  agents: [
    {
      id: "agent-core",
      name: "EnterpriseAssistantAgent",
      role: "orchestrator",
      description: "Agente orquestador principal del sistema empresarial OCAP (CAP-03).",
      status: "idle",
      activeModel: "OpenAI (gpt-4o)",
      toolsCount: 14,
      executionsCount: 12450,
      successRate: 99.8,
    },
    {
      id: "agent-doc",
      name: "DocumentParserAgent",
      role: "specialist",
      description: "Agente especializado en análisis de contratos, PDFs y extracción de datos.",
      status: "idle",
      activeModel: "Gemini (gemini-1.5-pro)",
      toolsCount: 6,
      executionsCount: 1840,
      successRate: 98.5,
    },
    {
      id: "agent-support",
      name: "CustomerSupportAgent",
      role: "specialist",
      description: "Atención omnichannel automatizada para resolución de tickets recurrentes.",
      status: "idle",
      activeModel: "Ollama (llama3:70b)",
      toolsCount: 8,
      executionsCount: 4210,
      successRate: 99.2,
    },
  ],
  recentTraces: [
    {
      id: "tr-1",
      agentName: "EnterpriseAssistantAgent",
      action: "Resolved User Intent",
      thought: "El usuario solicita agendar reunión con soporte. Invocando herramienta Calendar.ScheduleMeeting.",
      toolUsed: "Calendar.ScheduleMeeting",
      status: "success",
      timestamp: "14:22:05",
    },
    {
      id: "tr-2",
      agentName: "DocumentParserAgent",
      action: "Extracted Invoice Data",
      thought: "Documento en formato PDF procesado. Se identificó el monto total de $1,450.00 USD.",
      toolUsed: "PdfExtractorTool",
      status: "success",
      timestamp: "14:18:30",
    },
  ],
};

export function useAgentsData() {
  return useQuery<AgentsData>({
    queryKey: ["agentsData"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_AGENTS_DATA;
    },
    staleTime: 30000,
  });
}
