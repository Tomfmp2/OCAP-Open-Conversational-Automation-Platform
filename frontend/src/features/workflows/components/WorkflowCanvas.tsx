"use client";

import React from "react";
import { GitFork, ArrowRight, Play, CheckCircle2, Save, CheckSquare, AlertCircle } from "lucide-react";
import { WorkflowNode, useWorkflowsData } from "../api/useWorkflowsData";
import { Surface } from "@/shared/components/ui/Surface";
import { Button } from "@/shared/components/ui/Button";
import { EmptyState } from "@/shared/components/ui/EmptyState";

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
 setExecutionResult({ success: true, message: "Validación correcta: la estructura del workflow es válida." });
 } catch (err: unknown) {
 setErrorMessage(err instanceof Error ? err.message : "Error al validar el workflow.");
 }
 };

 const handleSave = async () => {
 setErrorMessage(null);
 setExecutionResult(null);
 try {
 await saveWorkflowMutation.mutateAsync({ id: workflowId, name: workflowName, nodes });
 setExecutionResult({ success: true, message: "Workflow guardado." });
 } catch (err: unknown) {
 setErrorMessage(err instanceof Error ? err.message : "Error al guardar el workflow.");
 }
 };

 const handleExecute = async () => {
 if (!workflowId) {
 setErrorMessage("No se puede ejecutar un workflow sin un identificador válido.");
 return;
 }
 setErrorMessage(null);
 setExecutionResult(null);
 try {
 const result = await executeWorkflowMutation.mutateAsync(workflowId);
 setExecutionResult({ success: true, message: `Ejecución real completada. ID de ejecución: ${result?.id || 'OK'}` });
 } catch (err: unknown) {
 setErrorMessage(err instanceof Error ? err.message : "Error al ejecutar el workflow en el motor backend.");
 }
 };

 const isLoading = validateWorkflowMutation.isPending || saveWorkflowMutation.isPending || executeWorkflowMutation.isPending;

 return (
 <Surface variant="glass" className="space-y-4">
 <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-neutral-100 pb-3">
 <div className="flex items-center gap-2">
 <GitFork className="w-4 h-4 text-neutral-700" />
 <h2 className="text-sm font-semibold text-neutral-950">
 Canvas — {workflowName}
 </h2>
 </div>
 <div className="flex items-center gap-2">
 {nodes.length > 0 && <Button
 type="button"
 variant="secondary"
 size="sm"
 onClick={handleValidate}
 loading={validateWorkflowMutation.isPending}
 disabled={isLoading}
 >
 <CheckSquare className="w-3.5 h-3.5" />
 <span>Validar</span>
 </Button>}

 {nodes.length > 0 && <Button
 type="button"
 variant="secondary"
 size="sm"
 onClick={handleSave}
 loading={saveWorkflowMutation.isPending}
 disabled={isLoading}
 >
 <Save className="w-3.5 h-3.5" />
 <span>Guardar</span>
 </Button>}

 <Button
 type="button"
 size="sm"
 onClick={handleExecute}
 loading={executeWorkflowMutation.isPending}
 disabled={isLoading || !workflowId}
 >
 <Play className="w-3.5 h-3.5" />
 <span>Ejecutar Flujo</span>
 </Button>
 </div>
 </div>

 {errorMessage && (
<div className="flex items-center gap-2 rounded-md border-2 border-neutral-950 bg-white p-3 text-xs text-neutral-950">
      <AlertCircle className="h-4 w-4 shrink-0" />
      <span>{errorMessage}</span>
    </div>
 )}

 {executionResult && (
 <div className="p-3 rounded-lg bg-neutral-100 border border-neutral-300 text-neutral-800 text-xs flex items-center gap-2">
 <CheckCircle2 className="w-4 h-4 shrink-0" />
 <span>{executionResult.message}</span>
 </div>
 )}

 {nodes.length === 0 ? (
 <EmptyState
 title="Estructura no disponible"
 description="La definición aún no tiene nodos. Ábrelos en el diseñador para editar."
 icon={<GitFork className="h-5 w-5" />}
 />
 ) : (
 <div className="flex flex-col md:flex-row items-center justify-between gap-3 p-4 bg-neutral-50 rounded-xl border border-neutral-200 overflow-x-auto">
 {nodes.map((node, index) => (
 <React.Fragment key={node.id}>
 <div className="flex-1 w-full p-3 bg-white border border-neutral-200 rounded-lg shadow-sm space-y-1">
 <div className="flex items-center justify-between text-[11px] text-neutral-500 font-mono">
 <span className="uppercase font-bold text-neutral-700">{node.type}</span>
 <span>Node #{index + 1}</span>
 </div>
 <p className="text-xs font-semibold text-neutral-950">{node.label}</p>
 <p className="text-[10px] text-neutral-500 font-mono truncate">{node.configSummary || "Configurado"}</p>
 </div>

 {index < nodes.length - 1 && (
 <ArrowRight className="w-5 h-5 text-neutral-500 shrink-0 hidden md:block" />
 )}
 </React.Fragment>
 ))}
 </div>
 )}
 </Surface>
 );
}
