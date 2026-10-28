"use client";

import React from "react";
import { Plus } from "lucide-react";
import { DEFAULT_AGENT_ID, useAgents, type Agent } from "@/features/agents/api/useAgentsData";
import { AgentFormModal } from "@/features/agents/components/AgentFormModal";
import { AgentTestPanel } from "@/features/agents/components/AgentTestPanel";
import { Badge, Button, ErrorState, ListSkeleton, Page, PageHeader } from "@/shared/components/ui";

export default function AgentsPage() {
  const agents = useAgents();
  const [editing, setEditing] = React.useState<Agent | null>(null);
  const [formOpen, setFormOpen] = React.useState(false);
  const [testing, setTesting] = React.useState<{ id: string; name: string } | null>(null);

  const openForm = (agent: Agent | null) => {
    setEditing(agent);
    setFormOpen(true);
  };

  return (
    <Page>
      <PageHeader
        title="Agentes"
        description="Cada agente responde con sus propias instrucciones. Asígnalos a un canal en Canales."
        actions={
          <Button onClick={() => openForm(null)}>
            <Plus className="h-4 w-4" aria-hidden /> Crear agente
          </Button>
        }
      />

      {agents.isLoading ? (
        <ListSkeleton label="Cargando agentes" />
      ) : agents.isError ? (
        <ErrorState message={agents.error instanceof Error ? agents.error.message : undefined} onRetry={() => void agents.refetch()} />
      ) : (
        <ul className="divide-y divide-neutral-200 rounded-lg border border-neutral-200 bg-white">
          <AgentRow
            name="Agente de atención"
            summary="Responde en los canales sin un agente asignado. Usa tu conocimiento y puede pasar la conversación a tu equipo."
            onTest={() => setTesting({ id: DEFAULT_AGENT_ID, name: "Agente de atención" })}
          />
          {agents.data?.map((agent) => (
            <AgentRow
              key={agent.id}
              name={agent.name}
              summary={agent.description || agent.systemPrompt}
              inactive={agent.status !== "Active"}
              onTest={() => setTesting({ id: agent.id, name: agent.name })}
              onEdit={() => openForm(agent)}
            />
          ))}
        </ul>
      )}

      <AgentFormModal open={formOpen} agent={editing} onClose={() => setFormOpen(false)} />
      <AgentTestPanel agent={testing} onClose={() => setTesting(null)} />
    </Page>
  );
}

function AgentRow({ name, summary, inactive, onTest, onEdit }: { name: string; summary: string; inactive?: boolean; onTest: () => void; onEdit?: () => void }) {
  return (
    <li className="flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:px-5">
      <div className="min-w-0 flex-1">
        <p className="flex items-center gap-2 text-sm font-medium text-neutral-950">
          {name}
          {inactive && <Badge>Inactivo</Badge>}
        </p>
        <p className="mt-0.5 line-clamp-2 text-sm text-neutral-600">{summary}</p>
      </div>
      <div className="flex gap-2">
        <Button variant="secondary" size="sm" onClick={onTest} aria-label={`Probar ${name}`}>
          Probar
        </Button>
        {onEdit && (
          <Button variant="secondary" size="sm" onClick={onEdit} aria-label={`Editar ${name}`}>
            Editar
          </Button>
        )}
      </div>
    </li>
  );
}
