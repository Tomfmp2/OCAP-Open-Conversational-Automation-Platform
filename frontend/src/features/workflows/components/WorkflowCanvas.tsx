"use client";

import React from "react";
import { GitFork, ArrowRight, Play, CheckCircle2, Zap, Radio, Bot } from "lucide-react";
import { WorkflowNode } from "../api/useWorkflowsData";

interface WorkflowCanvasProps {
  nodes: WorkflowNode[];
  workflowName: string;
}

export function WorkflowCanvas({ nodes, workflowName }: WorkflowCanvasProps) {
  const [simulating, setSimulating] = React.useState(false);
  const [simulationComplete, setSimulationComplete] = React.useState(false);

  const handleSimulate = () => {
    setSimulating(true);
    setSimulationComplete(false);
    setTimeout(() => {
      setSimulating(false);
      setSimulationComplete(true);
    }, 1200);
  };

  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <GitFork className="w-4 h-4 text-blue-500" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
            Canvas Studio Designer — {workflowName}
          </h2>
        </div>
        <button
          onClick={handleSimulate}
          disabled={simulating}
          className="flex items-center gap-1.5 px-3 py-1 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-sm transition-colors disabled:opacity-50"
        >
          <Play className={`w-3 h-3 ${simulating ? "animate-pulse" : ""}`} />
          <span>{simulating ? "Simulando Flujo..." : "Simular Ejecución"}</span>
        </button>
      </div>

      {simulationComplete && (
        <div className="p-3 rounded-lg bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-xs flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4" />
          <span>Simulación completada con éxito. Latencia total estimada: 142ms.</span>
        </div>
      )}

      {/* Visual Pipeline Nodes Flow */}
      <div className="flex flex-col md:flex-row items-center justify-between gap-3 p-4 bg-zinc-50 dark:bg-zinc-950/60 rounded-xl border border-zinc-200 dark:border-zinc-800/80 overflow-x-auto">
        {nodes.map((node, index) => (
          <React.Fragment key={node.id}>
            <div className="flex-1 w-full p-3 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg shadow-sm space-y-1">
              <div className="flex items-center justify-between text-[11px] text-zinc-400 font-mono">
                <span className="uppercase font-bold text-blue-500">{node.type}</span>
                <span>Node #{index + 1}</span>
              </div>
              <p className="text-xs font-semibold text-zinc-900 dark:text-zinc-100">{node.label}</p>
              <p className="text-[10px] text-zinc-400 font-mono truncate">{node.configSummary}</p>
            </div>

            {index < nodes.length - 1 && (
              <ArrowRight className="w-5 h-5 text-zinc-400 shrink-0 hidden md:block" />
            )}
          </React.Fragment>
        ))}
      </div>
    </div>
  );
}
