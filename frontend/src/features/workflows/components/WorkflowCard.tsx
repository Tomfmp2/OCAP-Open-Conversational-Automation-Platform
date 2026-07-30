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
      variant="glass"
      className={`cursor-pointer space-y-4 ${
        isSelected
          ? "border-blue-500 ring-1 ring-blue-500"
          : "hover:border-zinc-700"
      }`}
    >
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-purple-500/10 text-purple-500 flex items-center justify-center font-bold text-sm border border-purple-500/20">
            <GitFork className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{workflow.name}</h3>
              <span className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-zinc-200 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400">
                {workflow.version}
              </span>
            </div>
            <p className="text-xs text-zinc-400">Última actividad: {workflow.lastRun}</p>
          </div>
        </div>

        <Badge tone={workflow.status === "published" || workflow.status === "Active" ? "success" : "neutral"}>
          {(workflow.status === "published" || workflow.status === "Active") && <CheckCircle2 className="w-3.5 h-3.5" />}
          {workflow.status}
        </Badge>
      </div>

      <div className="text-xs pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <div>
          <span className="text-zinc-400 text-[10px]">Ejecuciones Totales</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">{workflow.totalExecutions.toLocaleString()}</p>
        </div>
      </div>
    </Surface>
  );
}
