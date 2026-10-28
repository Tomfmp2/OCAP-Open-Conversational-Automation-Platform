"use client";

import React from "react";
import { useAgents } from "@/features/agents/api/useAgentsData";
import { useAssignChannelAgent } from "../api/useChannelsData";

interface ChannelAgentSelectProps {
  connectionId: string;
  agentId: string | null;
}

/** Elige qué agente responde a los clientes de este canal. */
export function ChannelAgentSelect({ connectionId, agentId }: ChannelAgentSelectProps) {
  const agents = useAgents();
  const assign = useAssignChannelAgent();
  const id = `channel-agent-${connectionId}`;

  return (
    <div className="space-y-1">
      <label htmlFor={id} className="block text-xs font-medium text-neutral-600">
        Agente que responde
      </label>
      <select
        id={id}
        value={agentId ?? ""}
        disabled={agents.isLoading || assign.isPending}
        onChange={(e) => assign.mutate({ connectionId, agentId: e.target.value || null })}
        className="focus-ring w-full rounded-md border border-neutral-300 bg-white px-2.5 py-1.5 text-xs text-neutral-900 disabled:opacity-60"
      >
        <option value="">Agente de atención (por defecto)</option>
        {agents.data?.map((agent) => (
          <option key={agent.id} value={agent.id}>
            {agent.name}
          </option>
        ))}
      </select>
      {assign.isError && (
        <p role="alert" className="text-xs text-neutral-950">
          {assign.error instanceof Error ? assign.error.message : "No se pudo asignar el agente."}
        </p>
      )}
    </div>
  );
}
