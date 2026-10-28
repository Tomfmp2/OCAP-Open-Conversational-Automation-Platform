"use client";

import { FormError, Select } from "@/shared/components/ui";
import { useAgents } from "@/features/agents/api/useAgentsData";
import { useAssignChannelAgent } from "../api/useChannelsData";

/** Qué agente responde a los clientes de este canal. */
export function ChannelAgentSelect({ connectionId, agentId }: { connectionId: string; agentId: string | null }) {
  const agents = useAgents();
  const assign = useAssignChannelAgent();

  return (
    <div className="space-y-2">
      <Select
        label="Agente que responde"
        value={agentId ?? ""}
        disabled={agents.isLoading || assign.isPending}
        onChange={(e) => assign.mutate({ connectionId, agentId: e.target.value || null })}
      >
        <option value="">Agente de atención (por defecto)</option>
        {agents.data?.map((agent) => (
          <option key={agent.id} value={agent.id}>
            {agent.name}
          </option>
        ))}
      </Select>
      <FormError error={assign.error} />
    </div>
  );
}
