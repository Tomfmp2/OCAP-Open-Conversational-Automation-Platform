"use client";

import React from "react";
import { GitFork, Plus, RefreshCw, Inbox, Play, History } from "lucide-react";
import {
  useWorkflowsData,
  useWorkflowExecutions,
  WorkflowItem,
} from "@/features/workflows/api/useWorkflowsData";
import { WorkflowCard } from "@/features/workflows/components/WorkflowCard";
import { WorkflowCanvas } from "@/features/workflows/components/WorkflowCanvas";
import { WorkflowsSkeleton } from "@/features/workflows/components/WorkflowsSkeleton";

export default function WorkflowsPage() {
  const {
    data: workflows,
    isLoading,
    refetch,
    isFetching,
    executeWorkflowMutation,
    resumeWorkflowMutation,
    approveWorkflowMutation,
  } = useWorkflowsData();
  const [selectedWf, setSelectedWf] = React.useState<WorkflowItem | null>(null);
  const [selectedExecutionId, setSelectedExecutionId] = React.useState<string | null>(null);

  const workflowList = workflows || [];
  const activeWf = selectedWf || workflowList[0] || null;

  const { data: executions, refetch: refetchExecutions } = useWorkflowExecutions(activeWf?.id);

  if (isLoading) {
    return <WorkflowsSkeleton />;
  }

  const handleExecute = () => {
    if (!activeWf) return;
    executeWorkflowMutation.mutate(activeWf.id, {
      onSuccess: () => void refetchExecutions(),
    });
  };

  const handleResume = (executionId: string) => {
    resumeWorkflowMutation.mutate({ executionId }, { onSuccess: () => void refetchExecutions() });
  };

  const handleApprove = (executionId: string, approved: boolean) => {
    approveWorkflowMutation.mutate(
      { executionId, approved },
      { onSuccess: () => void refetchExecutions() }
    );
  };

  return (
    <div className="max-w-7xl mx-auto space-y-6">
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
            type="button"
            onClick={() => refetch()}
            disabled={isFetching}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors shadow-sm disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            <span>Actualizar</span>
          </button>
          {activeWf && (
            <button
              type="button"
              onClick={handleExecute}
              disabled={executeWorkflowMutation.isPending}
              className="flex items-center gap-2 px-3.5 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-semibold shadow-md transition-colors disabled:opacity-50"
            >
              <Play className="w-3.5 h-3.5" />
              <span>{executeWorkflowMutation.isPending ? "Ejecutando..." : "Ejecutar"}</span>
            </button>
          )}
          <button
            type="button"
            className="flex items-center gap-2 px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Nuevo Workflow</span>
          </button>
        </div>
      </div>

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

            {activeWf && (
              <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 shadow-sm space-y-3">
                <div className="flex items-center gap-2">
                  <History className="w-4 h-4 text-blue-500" />
                  <h3 className="text-xs font-semibold text-zinc-900 dark:text-zinc-100">Historial de Ejecuciones</h3>
                </div>
                {(executions || []).length === 0 ? (
                  <p className="text-[11px] text-zinc-500">Sin ejecuciones registradas.</p>
                ) : (
                  <div className="space-y-2 max-h-48 overflow-y-auto">
                    {(executions || []).slice(0, 10).map((exec) => (
                      <div
                        key={exec.id}
                        className="p-2 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 text-[10px] space-y-1.5"
                      >
                        <div className="flex items-center justify-between">
                          <button
                            type="button"
                            className="font-mono text-zinc-400 hover:text-blue-500"
                            onClick={() => setSelectedExecutionId(exec.id)}
                          >
                            {exec.id.slice(0, 8)}...
                          </button>
                          <span className="font-semibold text-zinc-700 dark:text-zinc-300">{exec.status}</span>
                        </div>
                        <p className="text-zinc-500">
                          {new Date(exec.startedAtUtc).toLocaleString()} · paso {exec.currentStepId}
                        </p>
                        {exec.status === "Paused" && (
                          <div className="flex flex-wrap gap-1">
                            <button
                              type="button"
                              onClick={() => handleResume(exec.id)}
                              className="px-2 py-0.5 rounded bg-blue-600 text-white font-semibold"
                            >
                              Resume
                            </button>
                            <button
                              type="button"
                              onClick={() => handleApprove(exec.id, true)}
                              className="px-2 py-0.5 rounded bg-emerald-600 text-white font-semibold"
                            >
                              Approve
                            </button>
                            <button
                              type="button"
                              onClick={() => handleApprove(exec.id, false)}
                              className="px-2 py-0.5 rounded bg-rose-600 text-white font-semibold"
                            >
                              Reject
                            </button>
                          </div>
                        )}
                        {selectedExecutionId === exec.id && (
                          <p className="text-zinc-400 font-mono truncate">{exec.outputJson}</p>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>

          <div className="lg:col-span-2">
            {activeWf && <WorkflowCanvas nodes={activeWf.nodes || []} workflowName={activeWf.name} />}
          </div>
        </div>
      )}
    </div>
  );
}
