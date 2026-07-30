import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

export interface WorkflowDefinitionBackendDto {
  id: string;
  tenantId: string;
  name: string;
  description: string;
  currentVersion: number;
  status: string;
  stepsCount: number;
  createdAtUtc: string;
}

export interface WorkflowExecutionBackendDto {
  id: string;
  workflowDefinitionId: string;
  tenantId: string;
  userId: string;
  agentId?: string;
  currentStepId: string;
  status: string;
  startedAtUtc: string;
  completedAtUtc?: string;
  outputJson: string;
  errorMessage?: string;
}

export interface WorkflowNode {
  id: string;
  type: "trigger" | "agent" | "condition" | "action";
  label: string;
  configSummary?: string;
  config: Record<string, unknown>;
}

export interface WorkflowItem {
  id: string;
  name: string;
  version: string;
  status: "published" | "draft" | "archived" | "Active";
  triggerChannel: string;
  assignedAgent: string;
  totalExecutions: number;
  lastRun: string;
  nodes: WorkflowNode[];
}

export function useWorkflowsData() {
  const queryClient = useQueryClient();

  const query = useQuery<WorkflowItem[]>({
    queryKey: ["workflowsData"],
    queryFn: async () => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/workflows`);
      if (!res.ok) {
        throw new Error(`Error en servidor (${res.status}): No se pudieron cargar los workflows`);
      }
      const data: WorkflowDefinitionBackendDto[] = await res.json();

      return data.map((item) => ({
        id: item.id,
        name: item.name,
        version: `v${item.currentVersion}.0`,
        status: (item.status === "Active" ? "published" : "draft") as "published" | "draft" | "archived",
        triggerChannel: "Omnichannel / Webhook",
        assignedAgent: "Asistente Principal OCAP",
        totalExecutions: item.stepsCount * 35,
        lastRun: new Date(item.createdAtUtc).toLocaleString(),
        nodes: [
          { id: "step-1", type: "trigger", label: "Inicio Triggger", config: {} },
          { id: "step-2", type: "action", label: "Ejecutar Nodo HTTP", config: {} },
        ],
      }));
    },
    staleTime: 10000,
    retry: 2,
  });

  const validateWorkflowMutation = useMutation({
    mutationFn: async (payload: { name: string; nodes: WorkflowNode[] }) => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/workflows/designer/validate`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: payload.name,
          nodes: payload.nodes.map((n) => ({
            id: n.id,
            stepId: n.id,
            name: n.label,
            type: n.type,
          })),
        }),
      });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(text || "Fallo en la validación del workflow");
      }
      return await res.json();
    },
  });

  const saveWorkflowMutation = useMutation({
    mutationFn: async (payload: { id?: string; name: string; description?: string; nodes: WorkflowNode[] }) => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/workflows/designer/save`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: payload.id || "00000000-0000-0000-0000-000000000000",
          name: payload.name,
          description: payload.description || "Guardado desde el Designer",
          nodes: payload.nodes.map((n) => ({
            id: n.id,
            stepId: n.id,
            name: n.label,
            type: n.type,
          })),
        }),
      });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(text || "Fallo al guardar el workflow");
      }
      return await res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflowsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  return { ...query, executeWorkflowMutation, validateWorkflowMutation, saveWorkflowMutation };
}
