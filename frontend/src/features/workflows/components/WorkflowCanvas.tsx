"use client";

import React from "react";
import { GitFork, ArrowRight, Play, CheckCircle2, Save, CheckSquare, Loader2, AlertCircle } from "lucide-react";
import { WorkflowNode, useWorkflowsData } from "../api/useWorkflowsData";

interface WorkflowCanvasProps {
  nodes: WorkflowNode[];
  workflowName: string;
  workflowId?: string;
}

export function WorkflowCanvas({ nodes, workflowName, workflowId }: WorkflowCanvasProps) {
  const [executionResult, setExecutionResult] = React.useState<{ success: boolean; message: string } | null>(null);
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

  const { validateWorkflowMutation, saveWorkflowMutation, executeWorkflowMutation } = useWorkflowsData();

  const handleValidate = async () => {
    setErrorMessage(null);
    setExecutionResult(null);
    try {
      await validateWorkflowMutation.mutateAsync({ name: workflowName, nodes });
      setExecutionResult({ success: true, message: "Validación exitosa: Estructura del workflow sin errores sintácticos ni ciclos infalibles." });
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Error al validar el workflow.");
    }
  };

  const handleSave = async () => {
    setErrorMessage(null);
    setExecutionResult(null);
    try {
      await saveWorkflowMutation.mutateAsync({ id: workflowId, name: workflowName, nodes });
      setExecutionResult({ success: true, message: "Workflow guardado exitosamente en PostgreSQL." });
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Error al guardar el workflow.");
    }
  };

  const handleExecute = async () => {
    setErrorMessage(null);
    setExecutionResult(null);
    try {
      const result = await executeWorkflowMutation.mutateAsync(workflowId || "00000000-0000-0000-0000-000000000000");
      setExecutionResult({ success: true, message: `Ejecución real completada. ID de ejecución: ${result?.id || 'OK'}` });
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Error al ejecutar el workflow en el motor backend.");
    }
  };

  const isLoading = validateWorkflowMutation.isPending || saveWorkflowMutation.isPending || executeWorkflowMutation.isPending;

  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <GitFork className="w-4 h-4 text-blue-500" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
            Canvas Studio Designer — {workflowName}
          </h2>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={handleValidate}
            disabled={isLoading}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-zinc-100 dark:bg-zinc-800 hover:bg-zinc-200 dark:hover:bg-zinc-700 text-zinc-800 dark:text-zinc-200 text-xs font-semibold transition-colors disabled:opacity-50"
          >
            <CheckSquare className="w-3.5 h-3.5" />
            <span>Validar</span>
          </button>

          <button
            onClick={handleSave}
            disabled={isLoading}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-zinc-100 dark:bg-zinc-800 hover:bg-zinc-200 dark:hover:bg-zinc-700 text-zinc-800 dark:text-zinc-200 text-xs font-semibold transition-colors disabled:opacity-50"
          >
            <Save className="w-3.5 h-3.5" />
            <span>Guardar</span>
          </button>

          <button
            onClick={handleExecute}
            disabled={isLoading}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-sm transition-colors disabled:opacity-50"
          >
            {isLoading ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Play className="w-3.5 h-3.5" />}
            <span>Ejecutar Flujo</span>
          </button>
        </div>
      </div>

      {errorMessage && (
        <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-500 text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      {executionResult && (
        <div className="p-3 rounded-lg bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-xs flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 shrink-0" />
          <span>{executionResult.message}</span>
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
              <p className="text-[10px] text-zinc-400 font-mono truncate">{node.configSummary || "Configurado"}</p>
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
