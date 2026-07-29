import React from "react";
import { Terminal, Wrench, Clock } from "lucide-react";
import { ReasoningStep } from "../api/useAgentsData";

interface ReasoningTraceInspectorProps {
  traces: ReasoningStep[];
}

export function ReasoningTraceInspector({ traces }: ReasoningTraceInspectorProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <Terminal className="w-4 h-4 text-blue-500" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
            Inspector de Trazas de Razonamiento (Execution Traces)
          </h2>
        </div>
        <span className="text-xs font-mono text-zinc-400">Live Agent Debugger</span>
      </div>

      <div className="space-y-3">
        {traces.map((step) => (
          <div
            key={step.id}
            className="p-4 rounded-xl bg-zinc-950 text-zinc-100 font-mono text-xs border border-zinc-800 space-y-2"
          >
            <div className="flex items-center justify-between text-[11px] text-zinc-400 border-b border-zinc-850 pb-2">
              <span className="text-blue-400 font-bold">[{step.agentName}]</span>
              <span className="flex items-center gap-1">
                <Clock className="w-3 h-3 text-zinc-500" />
                {step.timestamp}
              </span>
            </div>

            <p className="text-emerald-400 font-semibold">&gt; Accion: {step.action}</p>
            <p className="text-zinc-300 leading-relaxed pl-3 border-l-2 border-zinc-700">{step.thought}</p>

            {step.toolUsed && (
              <div className="flex items-center gap-2 text-[11px] text-amber-400 pt-1">
                <Wrench className="w-3 h-3" />
                <span>Tool Executed: {step.toolUsed}</span>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
