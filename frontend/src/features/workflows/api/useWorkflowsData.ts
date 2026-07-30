import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

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

export interface WorkflowStatusDto {
  workflowId: string;
  status: string;
  totalExecutions: number;
  successfulExecutions: number;
  failedExecutions: number;
  successRatePercentage: number;
  lastExecutedAtUtc?: string | null;
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

async function fetchWorkflowStatus(workflowId: string): Promise<WorkflowStatusDto | null> {
  try {
    return await apiClient.get<WorkflowStatusDto>(`/api/workflows/${workflowId}/status`);
  } catch {
    return null;
  }
}

export function useWorkflowsData() {
  const queryClient = useQueryClient();

  const query = useQuery<WorkflowItem[]>({
    queryKey: ["workflowsData"],
    queryFn: async () => {
      const data = await apiClient.get<WorkflowDefinitionBackendDto[]>("/api/workflows");

      const items = await Promise.all(
        data.map(async (item) => {
          const statusInfo = await fetchWorkflowStatus(item.id);

          return {
            id: item.id,
            name: item.name,
            version: `v${item.currentVersion}.0`,
            status: (item.status === "Active" ? "published" : "draft") as
              | "published"
              | "draft"
              | "archived",
            triggerChannel: "N/D",
            assignedAgent: "—",
            totalExecutions: statusInfo?.totalExecutions ?? 0,
            lastRun: statusInfo?.lastExecutedAtUtc
              ? new Date(statusInfo.lastExecutedAtUtc).toLocaleString()
              : new Date(item.createdAtUtc).toLocaleString(),
            // El listado no expone la definición de pasos. No se fabrican nodos.
            nodes: [],
          };
        })
      );

      return items;
    },
    staleTime: 10000,
    retry: 2,
  });

  const executeWorkflowMutation = useMutation({
    mutationFn: async (workflowId: string) => {
      return apiClient.post<WorkflowExecutionBackendDto>(
        `/api/workflows/${workflowId}/execute`
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflowsData"] });
      queryClient.invalidateQueries({ queryKey: ["workflowExecutions"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  const cancelWorkflowMutation = useMutation({
    mutationFn: async (workflowId: string) => {
      return apiClient.post<WorkflowExecutionBackendDto>(
        `/api/workflows/${workflowId}/cancel`
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflowsData"] });
      queryClient.invalidateQueries({ queryKey: ["workflowExecutions"] });
    },
  });

  const resumeWorkflowMutation = useMutation({
    mutationFn: async (payload: {
      executionId: string;
      signal?: string;
      payloadJson?: string;
    }) => {
      return apiClient.post<WorkflowExecutionBackendDto>(
        `/api/workflows/executions/${payload.executionId}/resume`,
        {
          signal: payload.signal ?? null,
          payloadJson: payload.payloadJson ?? null,
        }
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflowsData"] });
      queryClient.invalidateQueries({ queryKey: ["workflowExecutions"] });
      queryClient.invalidateQueries({ queryKey: ["workflowExecutionHistory"] });
    },
  });

  const approveWorkflowMutation = useMutation({
    mutationFn: async (payload: { executionId: string; approved: boolean }) => {
      return apiClient.post<WorkflowExecutionBackendDto>(
        `/api/workflows/executions/${payload.executionId}/signal`,
        {
          signal: payload.approved ? "approved" : "rejected",
          payloadJson: null,
        }
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflowExecutions"] });
      queryClient.invalidateQueries({ queryKey: ["workflowExecutionHistory"] });
    },
  });

  const validateWorkflowMutation = useMutation({
    mutationFn: async (payload: { name: string; nodes: WorkflowNode[] }) => {
      return apiClient.post("/api/workflows/designer/validate", {
        name: payload.name,
        nodes: payload.nodes.map((n) => ({
          id: n.id,
          stepId: n.id,
          name: n.label,
          type: n.type,
        })),
      });
    },
  });

  const saveWorkflowMutation = useMutation({
    mutationFn: async (payload: {
      id?: string;
      name: string;
      description?: string;
      nodes: WorkflowNode[];
    }) => {
      return apiClient.post("/api/workflows/designer/save", {
        id: payload.id || "00000000-0000-0000-0000-000000000000",
        name: payload.name,
        description: payload.description || "Guardado desde el Designer",
        nodes: payload.nodes.map((n) => ({
          id: n.id,
          stepId: n.id,
          name: n.label,
          type: n.type,
        })),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["workflowsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  return {
    ...query,
    executeWorkflowMutation,
    cancelWorkflowMutation,
    resumeWorkflowMutation,
    approveWorkflowMutation,
    validateWorkflowMutation,
    saveWorkflowMutation,
  };
}

export function useWorkflowExecutions(workflowId?: string) {
  return useQuery<WorkflowExecutionBackendDto[]>({
    queryKey: ["workflowExecutions", workflowId],
    queryFn: async () => {
      if (workflowId) {
        return apiClient.get<WorkflowExecutionBackendDto[]>(
          `/api/workflows/${workflowId}/executions`
        );
      }
      return apiClient.get<WorkflowExecutionBackendDto[]>("/api/workflows/executions");
    },
    enabled: Boolean(workflowId),
    staleTime: 10000,
  });
}

export function useWorkflowExecutionHistory(executionId?: string) {
  return useQuery({
    queryKey: ["workflowExecutionHistory", executionId],
    queryFn: async () => {
      if (!executionId) return [];
      return apiClient.get<
        Array<{
          id: string;
          stepId: string;
          stepName: string;
          nodeType: string;
          status: string;
          durationMs: number;
          outputJson: string;
          errorMessage?: string;
          executedAtUtc: string;
        }>
      >(`/api/workflows/executions/${executionId}/history`);
    },
    enabled: Boolean(executionId),
  });
}
