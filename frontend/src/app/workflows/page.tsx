"use client";

import React from "react";
import { GitFork, Plus, RefreshCw, Inbox } from "lucide-react";
import { useWorkflowsData, WorkflowItem } from "@/features/workflows/api/useWorkflowsData";
import { WorkflowCard } from "@/features/workflows/components/WorkflowCard";
import { WorkflowCanvas } from "@/features/workflows/components/WorkflowCanvas";
import { WorkflowsSkeleton } from "@/features/workflows/components/WorkflowsSkeleton";

export default function WorkflowsPage() {
  const { data: workflows, isLoading, refetch, isFetching } = useWorkflowsData();
  const [selectedWf, setSelectedWf] = React.useState<WorkflowItem | null>(null);

  const workflowList = workflows || [];

  React.useEffect(() => {
    if (workflowList.length > 0 && !selectedWf) {
      setSelectedWf(workflowList[0]);
    }
  }, [workflowList, selectedWf]);

  if (isLoading) {
    return <WorkflowsSkeleton />;
  }

  const activeWf = selectedWf || workflowList[0];

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <GitFork className="w-5 h-5 text-purple-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Workflow Studio (Diseñador de Flujos & Versionado)
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Crea flujos visuales de automatización empresarial conectando triggers de canales y razonamiento de agentes.
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
          <button className="flex items-center gap-2 px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors">
            <Plus className="w-3.5 h-3.5" />
            <span>Nuevo Workflow</span>
          </button>
        </div>
      </div>

      {/* Main Studio Grid or Empty State */}
      {workflowList.length === 0 ? (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-12 text-center space-y-4">
          <div className="w-12 h-12 rounded-full bg-zinc-100 dark:bg-zinc-800 text-zinc-400 mx-auto flex items-center justify-center">
            <Inbox className="w-6 h-6" />
          </div>
          <div>
            <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-100">No hay flujos de trabajo (workflows) disponibles</h3>
            <p className="text-xs text-zinc-500 mt-1">
              Diseña un nuevo flujo visual conectando canales de entrada con respuestas automatizadas de agentes.
            </p>
          </div>
          <button className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors">
            <Plus className="w-4 h-4" />
            <span>Nuevo Workflow</span>
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="space-y-4">
            <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Flujos Publicados</h2>
            {workflowList.map((wf) => (
              <WorkflowCard
                key={wf.id}
                workflow={wf}
                onSelect={setSelectedWf}
                isSelected={activeWf?.id === wf.id}
              />
            ))}
          </div>

          <div className="lg:col-span-2">
            {activeWf && <WorkflowCanvas nodes={activeWf.nodes || []} workflowName={activeWf.name} />}
          </div>
        </div>
      )}
    </div>
  );
}
