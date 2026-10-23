"use client";

import React from "react";
import { Bot, MessageSquareText, Plus, RefreshCw } from "lucide-react";
import { DEFAULT_AGENT_ID, useAgents, useDeleteAgent, type Agent } from "@/features/agents/api/useAgentsData";
import { AgentCard } from "@/features/agents/components/AgentCard";
import { AgentFormModal } from "@/features/agents/components/AgentFormModal";
import { AgentTestPanel } from "@/features/agents/components/AgentTestPanel";
import { AgentsSkeleton } from "@/features/agents/components/AgentsSkeleton";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Button } from "@/shared/components/ui/Button";
import { EmptyState } from "@/shared/components/ui/EmptyState";
import { ErrorState } from "@/shared/components/ui/ErrorState";
import { Surface } from "@/shared/components/ui/Surface";

const DEFAULT_AGENT = { id: DEFAULT_AGENT_ID, name: "Agente de atención (por defecto)" };

export default function AgentsPage() {
  const { data: agents, isLoading, isError, error, refetch, isFetching } = useAgents();
  const remove = useDeleteAgent();
  const [editing, setEditing] = React.useState<Agent | null>(null);
  const [formOpen, setFormOpen] = React.useState(false);
  const [testing, setTesting] = React.useState<{ id: string; name: string } | null>(null);
  const [deleteError, setDeleteError] = React.useState<string | null>(null);

  if (isLoading) return <AgentsSkeleton />;
  if (isError) {
    return (
      <div className="mx-auto max-w-7xl">
        <ErrorState message={error instanceof Error ? error.message : undefined} onRetry={() => void refetch()} />
      </div>
    );
  }

  const openCreate = () => {
    setEditing(null);
    setFormOpen(true);
  };

  const confirmDelete = async (agent: Agent) => {
    if (!window.confirm(`¿Eliminar el agente "${agent.name}"? Los canales que lo usen pasarán al agente por defecto.`)) return;
    setDeleteError(null);
    try {
      await remove.mutateAsync(agent.id);
    } catch (err: unknown) {
      setDeleteError(err instanceof Error ? err.message : "No se pudo eliminar el agente.");
    }
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Agentes"
        description="Agentes que responden a tus clientes en WhatsApp y Telegram. Asigna uno a cada canal en Canales."
        icon={<Bot className="h-5 w-5 text-neutral-700" />}
        actions={
          <>
            <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}>
              <RefreshCw className="h-3.5 w-3.5" aria-hidden /> Actualizar
            </Button>
            <Button size="sm" onClick={openCreate}>
              <Plus className="h-3.5 w-3.5" aria-hidden /> Crear agente
            </Button>
          </>
        }
      />

      <Surface className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-sm font-semibold text-neutral-950">Agente de atención por defecto</h2>
          <p className="text-xs text-neutral-600">
            Responde en los canales que no tienen un agente asignado: amable, breve, usa tu base de conocimiento y
            puede pasar la conversación a tu equipo.
          </p>
        </div>
        <Button variant="secondary" size="sm" onClick={() => setTesting(DEFAULT_AGENT)}>
          <MessageSquareText className="h-3.5 w-3.5" aria-hidden /> Probar
        </Button>
      </Surface>

      {deleteError && (
        <p role="alert" className="text-xs text-neutral-950">
          {deleteError}
        </p>
      )}

      {!agents || agents.length === 0 ? (
        <EmptyState
          title="Todavía no creaste agentes"
          description="Crea uno con sus instrucciones (por ejemplo, Ventas o Soporte) y asígnalo a un canal."
          action={
            <Button size="sm" onClick={openCreate}>
              <Plus className="h-4 w-4" aria-hidden /> Crear agente
            </Button>
          }
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          {agents.map((agent) => (
            <AgentCard
              key={agent.id}
              agent={agent}
              onTest={() => setTesting({ id: agent.id, name: agent.name })}
              onEdit={() => {
                setEditing(agent);
                setFormOpen(true);
              }}
              onDelete={() => void confirmDelete(agent)}
              deleting={remove.isPending && remove.variables === agent.id}
            />
          ))}
        </div>
      )}

      <AgentFormModal open={formOpen} agent={editing} onClose={() => setFormOpen(false)} />
      <AgentTestPanel agent={testing} onClose={() => setTesting(null)} />
    </div>
  );
}
