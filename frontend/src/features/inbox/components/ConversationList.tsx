import { cn } from "@/shared/utils/cn";
import type { InboxConversation } from "../api/useInbox";

interface ConversationListProps {
  conversations: InboxConversation[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}

const SENDER_PREFIX: Record<string, string> = { Agent: "Agente: ", Operator: "Equipo: " };

export function formatTime(iso: string) {
  const date = new Date(iso);
  return date.toDateString() === new Date().toDateString()
    ? date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
    : date.toLocaleDateString([], { day: "numeric", month: "short" });
}

export function ConversationList({ conversations, selectedId, onSelect }: ConversationListProps) {
  return (
    <ul className="divide-y divide-neutral-200" aria-label="Conversaciones">
      {conversations.map((conversation) => {
        const selected = conversation.id === selectedId;
        return (
          <li key={conversation.id}>
            <button
              type="button"
              onClick={() => onSelect(conversation.id)}
              aria-current={selected ? "true" : undefined}
              className={cn("focus-ring flex w-full flex-col gap-0.5 px-4 py-3 text-left transition-colors", selected ? "bg-neutral-100" : "hover:bg-neutral-50")}
            >
              <span className="flex items-baseline justify-between gap-2">
                <span className="truncate text-sm font-medium text-neutral-950">{conversation.contactName}</span>
                <span className="shrink-0 text-xs text-neutral-600">{formatTime(conversation.lastActivityAt)}</span>
              </span>
              <span className="truncate text-sm text-neutral-600">
                {conversation.lastSender && SENDER_PREFIX[conversation.lastSender]}
                {conversation.lastMessage ?? "Sin mensajes"}
              </span>
              <span className="mt-1 flex items-center gap-2 text-xs text-neutral-600">
                {conversation.channel}
                {conversation.handledByHuman && <span className="font-medium text-warning">Atiende el equipo</span>}
              </span>
            </button>
          </li>
        );
      })}
    </ul>
  );
}
