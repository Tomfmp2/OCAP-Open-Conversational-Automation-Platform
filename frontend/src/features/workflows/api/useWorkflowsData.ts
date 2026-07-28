import { useQuery } from "@tanstack/react-query";

export interface WorkflowNode {
  id: string;
  type: "trigger" | "agent" | "condition" | "action";
  label: string;
  configSummary: string;
}

export interface WorkflowItem {
  id: string;
  name: string;
  version: string;
  status: "published" | "draft" | "archived";
  triggerChannel: string;
  assignedAgent: string;
  totalExecutions: number;
  lastRun: string;
  nodes: WorkflowNode[];
}

const MOCK_WORKFLOWS: WorkflowItem[] = [
  {
    id: "wf-1",
    name: "Onboarding Automático de Clientes Enterprise",
    version: "v2.1.0",
    status: "published",
    triggerChannel: "Telegram / WhatsApp",
    assignedAgent: "EnterpriseAssistantAgent",
    totalExecutions: 4890,
    lastRun: "Hace 5 min",
    nodes: [
      { id: "n1", type: "trigger", label: "Nuevo Mensaje Recibido", configSummary: "Filtro: Intent = LeadRegistration" },
      { id: "n2", type: "agent", label: "Ejecutar Agent Reasoning", configSummary: "EnterpriseAssistantAgent" },
      { id: "n3", type: "action", label: "Crear Registro CRM", configSummary: "API POST /leads" },
    ],
  },
  {
    id: "wf-2",
    name: "Extracción & Clasificación de Facturas PDF",
    version: "v1.4.0",
    status: "published",
    triggerChannel: "Gmail Workspace",
    assignedAgent: "DocumentParserAgent",
    totalExecutions: 1420,
    lastRun: "Hace 30 min",
    nodes: [
      { id: "n1", type: "trigger", label: "Correo Recibido con Adjunto", configSummary: "MIME: application/pdf" },
      { id: "n2", type: "agent", label: "Procesar Documento", configSummary: "DocumentParserAgent" },
      { id: "n3", type: "action", label: "Enviar Confirmación Slack", configSummary: "Webhook #finanzas" },
    ],
  },
];

export function useWorkflowsData() {
  return useQuery<WorkflowItem[]>({
    queryKey: ["workflowsData"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_WORKFLOWS;
    },
    staleTime: 30000,
  });
}
