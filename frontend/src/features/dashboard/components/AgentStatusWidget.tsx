import React from "react";
import { Bot, Cpu, ShieldCheck, Zap, HardDrive } from "lucide-react";

interface AgentStatusWidgetProps {
  agentStatus: {
    name: string;
    status: "idle" | "busy" | "error";
    activeProvider: string;
    memoryUsedMb: number;
    registeredTools: number;
  };
}

export function AgentStatusWidget({ agentStatus }: AgentStatusWidgetProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-xl bg-blue-600/10 text-blue-500">
            <Bot className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{agentStatus.name}</h2>
            <p className="text-[11px] text-zinc-400">Orquestador de Capacidades CAP-03</p>
          </div>
        </div>
        <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
          Operacional
        </span>
      </div>

      <div className="grid grid-cols-2 gap-3 text-xs">
        <div className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800">
          <div className="flex items-center gap-1.5 text-zinc-400 mb-1">
            <Cpu className="w-3.5 h-3.5" />
            <span>Proveedor IA Activo</span>
          </div>
          <p className="font-semibold text-zinc-900 dark:text-zinc-100 truncate">{agentStatus.activeProvider}</p>
        </div>

        <div className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800">
          <div className="flex items-center gap-1.5 text-zinc-400 mb-1">
            <Zap className="w-3.5 h-3.5" />
            <span>Herramientas Registradas</span>
          </div>
          <p className="font-semibold text-zinc-900 dark:text-zinc-100">{agentStatus.registeredTools} Tools</p>
        </div>

        <div className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800">
          <div className="flex items-center gap-1.5 text-zinc-400 mb-1">
            <HardDrive className="w-3.5 h-3.5" />
            <span>Memoria Contextual</span>
          </div>
          <p className="font-semibold text-zinc-900 dark:text-zinc-100">{agentStatus.memoryUsedMb} MB RAM</p>
        </div>

        <div className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800">
          <div className="flex items-center gap-1.5 text-zinc-400 mb-1">
            <ShieldCheck className="w-3.5 h-3.5" />
            <span>Failover Automatico</span>
          </div>
          <p className="font-semibold text-emerald-500">Habilitado</p>
        </div>
      </div>
    </div>
  );
}
