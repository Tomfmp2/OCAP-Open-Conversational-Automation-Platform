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
    <Surface variant="glass" className="space-y-4">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-600/10 text-blue-500 flex items-center justify-center font-bold text-sm border border-blue-500/20">
            <Bot className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{agent.name}</h3>
              <Badge tone="info">{agent.role}</Badge>
            </div>
            <p className="text-xs text-zinc-500 mt-0.5">{agent.description}</p>
          </div>
        </div>

        <Badge tone={agent.status === "error" ? "danger" : "neutral"}>{agent.status}</Badge>
      </div>

      <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <div>
          <span className="text-zinc-400 text-[10px]">Modelo Asignado</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200 truncate">{agent.activeModel}</p>
        </div>
        <div>
          <span className="text-zinc-400 text-[10px]">Herramientas</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">{agent.toolsCount}</p>
        </div>
      </div>
    </Surface>
  );
}
