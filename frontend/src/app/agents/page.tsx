"use client";

import React from "react";
import { Bot, Plus, RefreshCw } from "lucide-react";
import { useAgentsData, AgentDto } from "@/features/agents/api/useAgentsData";
import { AgentCard } from "@/features/agents/components/AgentCard";
import { ReasoningTraceInspector } from "@/features/agents/components/ReasoningTraceInspector";
import { CreateAgentModal } from "@/features/agents/components/CreateAgentModal";
import { AgentsSkeleton } from "@/features/agents/components/AgentsSkeleton";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Button } from "@/shared/components/ui/Button";
import { MetricCard } from "@/shared/components/ui/MetricCard";
import { EmptyState } from "@/shared/components/ui/EmptyState";
import { ErrorState } from "@/shared/components/ui/ErrorState";

export default function AgentsPage() {
 const { data, isLoading, isError, error, refetch, isFetching } = useAgentsData();
 const [modalOpen, setModalOpen] = React.useState(false);

 if (isLoading) {
 return <AgentsSkeleton />;
 }

 if (isError) {
 return <div className="mx-auto max-w-7xl"><ErrorState message={error instanceof Error ? error.message : undefined} onRetry={() => void refetch()} /></div>;
 }

 const { agents, recentTraces } = data || { agents: [], recentTraces: [] };

 return (
 <div className="mx-auto max-w-7xl space-y-6">
 <PageHeader
 title="Agentes"
 description="Agentes registrados en el runtime y sus herramientas disponibles."
 icon={<Bot className="h-5 w-5 text-neutral-700" />}
 actions={<>
 <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}><RefreshCw className="h-3.5 w-3.5" /> Actualizar</Button>
 <Button size="sm" onClick={() => setModalOpen(true)}><Plus className="h-3.5 w-3.5" /> Crear agente</Button>
 </>}
 />
 <div className="max-w-sm"><MetricCard title="Agentes registrados" value={agents.length} icon={Bot} tone="info" /></div>
 {agents.length === 0 ? (
 <EmptyState title="No hay agentes registrados" description="Crea un agente cuando tengas definido su propósito e instrucciones." action={<Button size="sm" onClick={() => setModalOpen(true)}><Plus className="h-4 w-4" /> Crear agente</Button>} />
 ) : (
 <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
 <div className="space-y-4">
 <h2 className="text-sm font-semibold text-neutral-950">Agentes disponibles</h2>
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
