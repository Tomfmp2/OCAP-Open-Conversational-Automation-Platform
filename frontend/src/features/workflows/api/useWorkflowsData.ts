import { useQuery } from "@tanstack/react-query";

export interface WorkflowNode {
  id: string;
  type: "trigger" | "agent" | "condition" | "action";
  label: string;
  configSummary?: string;
  config: Record<string, any>;
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

export function useWorkflowsData() {
  return useQuery<WorkflowItem[]>({
    queryKey: ["workflowsData"],
    queryFn: async () => {
      const res = await fetch("/api/workflows");
      if (!res.ok) {
        return [];
      }
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    },
    staleTime: 15000,
  });
}
