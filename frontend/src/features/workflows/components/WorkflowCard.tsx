import React from "react";
import { GitFork, CheckCircle2, Play, Radio, RotateCcw } from "lucide-react";
import { WorkflowItem } from "../api/useWorkflowsData";

interface WorkflowCardProps {
  workflow: WorkflowItem;
  onSelect: (wf: WorkflowItem) => void;
  isSelected: boolean;
}

export function WorkflowCard({ workflow, onSelect, isSelected }: WorkflowCardProps) {
  return (
    <div
      onClick={() => onSelect(workflow)}
      className={`bg-white dark:bg-zinc-900 border rounded-xl p-5 shadow-sm cursor-pointer space-y-4 transition-all ${
        isSelected
          ? "border-blue-500 ring-1 ring-blue-500"
          : "border-zinc-200 dark:border-zinc-800/80 hover:border-zinc-300 dark:hover:border-zinc-700"
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
            <p className="text-xs text-zinc-400">Agente: {workflow.assignedAgent}</p>
          </div>
        </div>

        <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 px-2.5 py-0.5 rounded-full border border-emerald-500/20">
          <CheckCircle2 className="w-3.5 h-3.5" /> Publicado
        </span>
      </div>

      <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <div>
          <span className="text-zinc-400 text-[10px]">Canal Trigger</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">{workflow.triggerChannel}</p>
        </div>
        <div>
          <span className="text-zinc-400 text-[10px]">Ejecuciones Totales</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">{workflow.totalExecutions.toLocaleString()}</p>
        </div>
      </div>
    </div>
  );
}
