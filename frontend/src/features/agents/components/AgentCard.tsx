import React from "react";
import { Bot, Cpu, Zap, CheckCircle2, ShieldCheck, Wrench } from "lucide-react";
import { AgentInfo } from "../api/useAgentsData";

interface AgentCardProps {
  agent: AgentInfo;
}

export function AgentCard({ agent }: AgentCardProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4 hover:border-zinc-300 dark:hover:border-zinc-700 transition-all">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-600/10 text-blue-500 flex items-center justify-center font-bold text-sm border border-blue-500/20">
            <Bot className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{agent.name}</h3>
              <span className="text-[10px] px-2 py-0.5 rounded-full bg-blue-500/10 text-blue-500 font-mono capitalize">
                {agent.role}
              </span>
            </div>
            <p className="text-xs text-zinc-500 mt-0.5">{agent.description}</p>
          </div>
        </div>

        <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 px-2.5 py-0.5 rounded-full border border-emerald-500/20 shrink-0">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" /> Ready
        </span>
      </div>

      <div className="grid grid-cols-3 gap-2 text-xs pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <div>
          <span className="text-zinc-400 text-[10px]">Modelo Asignado</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200 truncate">{agent.activeModel}</p>
        </div>
        <div>
          <span className="text-zinc-400 text-[10px]">Herramientas</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">{agent.toolsCount} Tools Attached</p>
        </div>
        <div>
          <span className="text-zinc-400 text-[10px]">Tasa de Éxito</span>
          <p className="font-semibold text-emerald-500">{agent.successRate}%</p>
        </div>
      </div>
    </div>
  );
}
