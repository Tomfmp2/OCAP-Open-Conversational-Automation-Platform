import { Bot, MessageSquareText, Pencil, Trash2, Wrench } from "lucide-react";
import type { Agent } from "../api/useAgentsData";
import { Surface } from "@/shared/components/ui/Surface";
import { Badge } from "@/shared/components/ui/Badge";
import { Button } from "@/shared/components/ui/Button";

interface AgentCardProps {
  agent: Agent;
  onTest: () => void;
  onEdit: () => void;
  onDelete: () => void;
  deleting: boolean;
}

export function AgentCard({ agent, onTest, onEdit, onDelete, deleting }: AgentCardProps) {
  return (
    <Surface className="flex flex-col gap-4">
      <div className="flex items-start gap-3">
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md border border-neutral-200 bg-neutral-50 text-neutral-800">
          <Bot className="h-5 w-5" aria-hidden />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-sm font-semibold text-neutral-950">{agent.name}</h3>
            <Badge tone={agent.status === "Active" ? "success" : "neutral"}>{agent.status === "Active" ? "Activo" : agent.status}</Badge>
          </div>
          {agent.description && <p className="mt-0.5 text-xs text-neutral-500">{agent.description}</p>}
          <p className="mt-2 line-clamp-3 text-xs text-neutral-700">{agent.systemPrompt}</p>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-1.5 text-xs text-neutral-600">
        <Wrench className="h-3.5 w-3.5" aria-hidden />
        {agent.enabledTools.length === 0 ? "Sin herramientas" : agent.enabledTools.map((t) => <Badge key={t}>{t}</Badge>)}
      </div>

      <div className="flex flex-wrap justify-end gap-2 border-t border-neutral-100 pt-3">
        <Button variant="ghost" size="sm" onClick={onDelete} loading={deleting} aria-label={`Eliminar ${agent.name}`}>
          <Trash2 className="h-3.5 w-3.5" aria-hidden />
        </Button>
        <Button variant="secondary" size="sm" onClick={onEdit}>
          <Pencil className="h-3.5 w-3.5" aria-hidden /> Editar
        </Button>
        <Button size="sm" onClick={onTest}>
          <MessageSquareText className="h-3.5 w-3.5" aria-hidden /> Probar
        </Button>
      </div>
    </Surface>
  );
}
