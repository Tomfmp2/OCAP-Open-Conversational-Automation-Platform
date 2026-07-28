"use client";

import React from "react";
import { Bot, Plus, RefreshCw, ShieldCheck, Zap, Inbox } from "lucide-react";
import { useAgentsData, AgentDto } from "@/features/agents/api/useAgentsData";
import { AgentCard } from "@/features/agents/components/AgentCard";
import { ReasoningTraceInspector } from "@/features/agents/components/ReasoningTraceInspector";
import { CreateAgentModal } from "@/features/agents/components/CreateAgentModal";
import { AgentsSkeleton } from "@/features/agents/components/AgentsSkeleton";

export default function AgentsPage() {
  const { data, isLoading, refetch, isFetching } = useAgentsData();
  const [modalOpen, setModalOpen] = React.useState(false);

  if (isLoading) {
    return <AgentsSkeleton />;
  }

  const { agents, recentTraces } = data || { agents: [], recentTraces: [] };

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <Bot className="w-5 h-5 text-blue-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Centro de Agentes & Runtime Pipeline (CAP-03)
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Orquestación de EnterpriseAssistantAgent y sub-agentes independientes de canales e infraestructura.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors shadow-sm disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            <span>Actualizar</span>
          </button>
          <button
            onClick={() => setModalOpen(true)}
            className="flex items-center gap-2 px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Crear Sub-Agente</span>
          </button>
        </div>
      </div>

      {/* KPI Strip */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 flex items-center justify-between shadow-sm">
          <div>
            <p className="text-xs text-zinc-500">Agentes Registrados</p>
            <p className="text-2xl font-bold text-zinc-900 dark:text-zinc-100 mt-1">{agents.length} Agentes</p>
          </div>
          <div className="p-2.5 rounded-xl bg-blue-50 dark:bg-blue-950/40 text-blue-500">
            <Bot className="w-5 h-5" />
          </div>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 flex items-center justify-between shadow-sm">
          <div>
            <p className="text-xs text-zinc-500">Ejecuciones Totales</p>
            <p className="text-2xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">0</p>
          </div>
          <div className="p-2.5 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 text-emerald-500">
            <Zap className="w-5 h-5" />
          </div>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 flex items-center justify-between shadow-sm">
          <div>
            <p className="text-xs text-zinc-500">Arquitectura Hexagonal</p>
            <p className="text-xs font-bold text-zinc-900 dark:text-zinc-100 mt-1">Independent Runtime</p>
          </div>
          <div className="p-2.5 rounded-xl bg-purple-50 dark:bg-purple-950/40 text-purple-500">
            <ShieldCheck className="w-5 h-5" />
          </div>
        </div>
      </div>

      {/* Main Grid: Agents List or Empty State */}
      {agents.length === 0 ? (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-12 text-center space-y-4">
          <div className="w-12 h-12 rounded-full bg-zinc-100 dark:bg-zinc-800 text-zinc-400 mx-auto flex items-center justify-center">
            <Inbox className="w-6 h-6" />
          </div>
          <div>
            <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-100">No hay agentes registrados</h3>
            <p className="text-xs text-zinc-500 mt-1">
              Crea un sub-agente especializado para delegar tareas avanzadas de razonamiento.
            </p>
          </div>
          <button
            onClick={() => setModalOpen(true)}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors"
          >
            <Plus className="w-4 h-4" />
            <span>Crear Sub-Agente</span>
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="space-y-4">
            <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Agentes en la Red</h2>
            {agents.map((agent: AgentDto) => (
              <AgentCard key={agent.id} agent={agent} />
            ))}
          </div>

          <div>
            <ReasoningTraceInspector traces={recentTraces} />
          </div>
        </div>
      )}

      <CreateAgentModal open={modalOpen} onClose={() => setModalOpen(false)} />
    </div>
  );
}
