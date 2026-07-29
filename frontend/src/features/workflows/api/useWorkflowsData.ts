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

  const executeWorkflowMutation = useMutation({
    mutationFn: async (workflowId: string) => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/workflows/${workflowId}/execute`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      });
      if (!res.ok) {
        throw new Error("Fallo al ejecutar el workflow en el motor backend");
      }
      return await res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflowsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  return { ...query, executeWorkflowMutation };
}
