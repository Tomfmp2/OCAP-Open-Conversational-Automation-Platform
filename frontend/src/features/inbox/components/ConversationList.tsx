import { UserRound } from "lucide-react";
import { Badge } from "@/shared/components/ui/Badge";
import { cn } from "@/shared/utils/cn";
import type { InboxConversation } from "../api/useInbox";

interface ConversationListProps {
  conversations: InboxConversation[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}

const SENDER_PREFIX: Record<string, string> = { Agent: "Agente: ", Operator: "Tú: " };

function formatTime(iso: string) {
  const date = new Date(iso);
  const sameDay = date.toDateString() === new Date().toDateString();
  return sameDay
    ? date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
    : date.toLocaleDateString([], { day: "2-digit", month: "short" });
}

export function ConversationList({ conversations, selectedId, onSelect }: ConversationListProps) {
  return (
    <ul className="divide-y divide-neutral-100" aria-label="Conversaciones">
      {conversations.map((conversation) => (
        <li key={conversation.id}>
          <button
            type="button"
            onClick={() => onSelect(conversation.id)}
            aria-current={conversation.id === selectedId ? "true" : undefined}
            className={cn(
              "focus-ring flex w-full flex-col gap-1 px-4 py-3 text-left transition-colors hover:bg-neutral-50",
              conversation.id === selectedId && "bg-neutral-100"
            )}
          >
            <span className="flex items-center justify-between gap-2">
              <span className="truncate text-sm font-semibold text-neutral-950">{conversation.contactName}</span>
              <span className="shrink-0 text-xs text-neutral-500">{formatTime(conversation.lastActivityAt)}</span>
            </span>
            <span className="truncate text-xs text-neutral-600">
              {conversation.lastSender && SENDER_PREFIX[conversation.lastSender]}
              {conversation.lastMessage ?? "Sin mensajes"}
            </span>
            <span className="flex flex-wrap gap-1.5">
              <Badge>{conversation.channel}</Badge>
              {conversation.handledByHuman && (
                <Badge tone="warning">
                  <UserRound className="h-3 w-3" aria-hidden /> Equipo
                </Badge>
              )}
            </span>
          </button>
        </li>
      ))}
    </ul>
  );
}
