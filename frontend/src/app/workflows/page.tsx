"use client";

import React from "react";
import { GitFork, RefreshCw, Play, History } from "lucide-react";
import {
 useWorkflowsData,
 useWorkflowExecutions,
 WorkflowItem,
} from "@/features/workflows/api/useWorkflowsData";
import { WorkflowCard } from "@/features/workflows/components/WorkflowCard";
import { WorkflowCanvas } from "@/features/workflows/components/WorkflowCanvas";
import { WorkflowsSkeleton } from "@/features/workflows/components/WorkflowsSkeleton";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Button } from "@/shared/components/ui/Button";
import { Surface } from "@/shared/components/ui/Surface";
import { EmptyState } from "@/shared/components/ui/EmptyState";
import { ErrorState } from "@/shared/components/ui/ErrorState";
import { Badge } from "@/shared/components/ui/Badge";

export default function WorkflowsPage() {
 const {
 data: workflows,
 isLoading,
 isError,
 error,
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

 const {
 data: executions,
 isError: executionsError,
 refetch: refetchExecutions,
 } = useWorkflowExecutions(activeWf?.id);

 if (isLoading) {
 return <WorkflowsSkeleton />;
 }

 if (isError) {
 return <div className="mx-auto max-w-7xl"><ErrorState message={error instanceof Error ? error.message : undefined} onRetry={() => void refetch()} /></div>;
 }

 const handleExecute = () => {
 if (!activeWf?.id) return;
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
 <div className="mx-auto max-w-7xl space-y-6">
 <PageHeader
 title="Workflows"
 description="Definiciones publicadas, ejecuciones y diseñador visual en Next.js."
 icon={<GitFork className="h-5 w-5 text-neutral-700" />}
 actions={<>
 <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}><RefreshCw className="h-3.5 w-3.5" /> Actualizar</Button>
 <a href="/workflows/designer" className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-3 py-1.5 text-xs font-semibold text-neutral-800 hover:border-neutral-400">Diseñar</a>
 {activeWf?.id && <Button size="sm" onClick={handleExecute} loading={executeWorkflowMutation.isPending}><Play className="h-3.5 w-3.5" /> Ejecutar</Button>}
 </>}
 />

 {workflowList.length === 0 ? (
 <EmptyState title="No hay workflows disponibles" description="El backend todavía no ha devuelto definiciones de workflow." />
 ) : (
 <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
 <div className="space-y-4">
 <h2 className="text-sm font-semibold text-neutral-950">Flujos Publicados</h2>
 {workflowList.map((wf) => (
 <WorkflowCard
 key={wf.id}
 workflow={wf}
 onSelect={setSelectedWf}
 isSelected={activeWf?.id === wf.id}
 />
 ))}

 {activeWf && (
 <Surface padding="md" className="space-y-3">
 <div className="flex items-center gap-2">
 <History className="w-4 h-4 text-neutral-700" />
 <h3 className="text-xs font-semibold text-neutral-950">Historial de Ejecuciones</h3>
 </div>
 {executionsError ? (
 <ErrorState title="No se pudo cargar el historial" onRetry={() => void refetchExecutions()} />
 ) : (executions || []).length === 0 ? (
 <p className="text-[11px] text-neutral-500">Sin ejecuciones registradas.</p>
 ) : (
 <div className="space-y-2 max-h-48 overflow-y-auto">
 {(executions || []).slice(0, 10).map((exec) => (
 <div
 key={exec.id}
 className="p-2 rounded-lg bg-neutral-50 border border-neutral-200 text-[10px] space-y-1.5"
 >
 <div className="flex items-center justify-between">
 <button
 type="button"
 className="font-mono text-neutral-500 hover:text-neutral-700"
 onClick={() => setSelectedExecutionId(exec.id)}
 >
 {exec.id.slice(0, 8)}...
 </button>
 <Badge tone={exec.status === "Paused" ? "warning" : exec.status === "Failed" ? "danger" : "neutral"}>{exec.status}</Badge>
 </div>
 <p className="text-neutral-500">
 {new Date(exec.startedAtUtc).toLocaleString()} · paso {exec.currentStepId}
 </p>
 {exec.status === "Paused" && (
 <div className="flex flex-wrap gap-1">
 <button
 type="button"
 onClick={() => handleResume(exec.id)}
 className="px-2 py-0.5 rounded bg-neutral-950 text-white font-semibold"
 >
 Resume
 </button>
 <button
 type="button"
 onClick={() => handleApprove(exec.id, true)}
 className="px-2 py-0.5 rounded bg-neutral-950 text-white font-semibold"
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
 <p className="text-neutral-500 font-mono truncate">{exec.outputJson}</p>
 )}
 </div>
 ))}
 </div>
 )}
 </Surface>
 )}
 </div>

 <div className="lg:col-span-2">
 {activeWf && (
 <WorkflowCanvas
 nodes={activeWf.nodes}
 workflowName={activeWf.name}
 workflowId={activeWf.id}
 />
 )}
 </div>
 </div>
 )}
 </div>
 );
}
