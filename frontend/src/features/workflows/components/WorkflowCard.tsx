import React from "react";
import { GitFork, CheckCircle2 } from "lucide-react";
import { WorkflowItem } from "../api/useWorkflowsData";
import { Surface } from "@/shared/components/ui/Surface";
import { Badge } from "@/shared/components/ui/Badge";

interface WorkflowCardProps {
  workflow: WorkflowItem;
  onSelect: (wf: WorkflowItem) => void;
  isSelected: boolean;
}

export function WorkflowCard({ workflow, onSelect, isSelected }: WorkflowCardProps) {
  return (
    <Surface
      role="button"
      tabIndex={0}
      onClick={() => onSelect(workflow)}
      onKeyDown={(event) => {
        if (event.key === "Enter" || event.key === " ") onSelect(workflow);
      }}
      className={`cursor-pointer space-y-4 ${
        isSelected
          ? "border-neutral-950 ring-1 ring-neutral-950"
          : "hover:border-neutral-400"
      }`}
    >
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-md border border-neutral-200 bg-neutral-50 text-neutral-800">
            <GitFork className="h-5 w-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-neutral-950">{workflow.name}</h3>
              <span className="rounded bg-neutral-200 px-1.5 py-0.5 font-mono text-[10px] text-neutral-600">
                {workflow.version}
              </span>
            </div>
            <p className="text-xs text-neutral-500">Última actividad: {workflow.lastRun}</p>
          </div>
        </div>

        <Badge
          tone={
            workflow.status === "published" || workflow.status === "Active"
              ? "success"
              : "neutral"
          }
        >
          {(workflow.status === "published" || workflow.status === "Active") && (
            <CheckCircle2 className="h-3.5 w-3.5" />
          )}
          {workflow.status}
        </Badge>
      </div>

      <div className="border-t border-neutral-100 pt-2 text-xs">
        <span className="text-[10px] text-neutral-500">Ejecuciones totales</span>
        <p className="font-semibold text-neutral-800">
          {workflow.totalExecutions.toLocaleString()}
        </p>
      </div>
    </Surface>
  );
}
