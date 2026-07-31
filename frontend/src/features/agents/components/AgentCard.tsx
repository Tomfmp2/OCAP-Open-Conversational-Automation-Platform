import React from "react";
import { Bot } from "lucide-react";
import { AgentInfo } from "../api/useAgentsData";
import { Surface } from "@/shared/components/ui/Surface";
import { Badge } from "@/shared/components/ui/Badge";

interface AgentCardProps {
  agent: AgentInfo;
}

export function AgentCard({ agent }: AgentCardProps) {
  return (
    <Surface className="space-y-4">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-md border border-neutral-200 bg-neutral-50 text-neutral-800">
            <Bot className="h-5 w-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-neutral-950">{agent.name}</h3>
              <Badge tone="info">{agent.role}</Badge>
            </div>
            <p className="mt-0.5 text-xs text-neutral-500">{agent.description}</p>
          </div>
        </div>
        <Badge tone={agent.status === "error" ? "danger" : "neutral"}>{agent.status}</Badge>
      </div>

      <div className="grid grid-cols-2 gap-2 border-t border-neutral-100 pt-2 text-xs">
        <div>
          <span className="text-[10px] text-neutral-500">Modelo asignado</span>
          <p className="truncate font-semibold text-neutral-800">{agent.activeModel}</p>
        </div>
        <div>
          <span className="text-[10px] text-neutral-500">Herramientas</span>
          <p className="font-semibold text-neutral-800">{agent.toolsCount}</p>
        </div>
      </div>
    </Surface>
  );
}
