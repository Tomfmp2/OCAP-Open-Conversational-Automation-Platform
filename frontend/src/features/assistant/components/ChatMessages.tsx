import type { ChatLine } from "../api/usePrincipalAgentChat";
import { AssistantOrb } from "./AssistantOrb";
import { TypewriterText } from "./TypewriterText";

interface ChatMessagesProps {
  lines: ChatLine[];
  pending: boolean;
  revealingId: string | null;
  onRevealDone: () => void;
}

/** Mensajes: los del usuario en burbuja oscura a la derecha; los del asistente como texto con su orbe. */
export function ChatMessages({ lines, pending, revealingId, onRevealDone }: ChatMessagesProps) {
  return (
    <ol className="mx-auto flex w-full max-w-3xl flex-col gap-6" aria-label="Conversación">
      {lines.map((line) =>
        line.role === "user" ? (
          <li key={line.id} className="ocap-hero-enter flex justify-end">
            <p className="max-w-[80%] whitespace-pre-wrap rounded-3xl rounded-br-lg bg-neutral-950 px-4 py-2.5 text-[15px] leading-relaxed text-white">
              {line.text}
            </p>
          </li>
        ) : (
          <li key={line.id} className="flex items-start gap-3">
            <AssistantOrb size={28} state={line.id === revealingId ? "speaking" : "idle"} className="mt-0.5" />
            <p className="min-w-0 flex-1 whitespace-pre-wrap pt-0.5 text-[15px] leading-relaxed text-neutral-900">
              {line.id === revealingId ? <TypewriterText text={line.text} onDone={onRevealDone} /> : line.text}
            </p>
          </li>
        )
      )}

      {pending && (
        <li className="flex items-center gap-3" aria-live="polite">
          <AssistantOrb size={28} state="thinking" />
          <span className="ocap-shimmer text-sm font-medium">Pensando…</span>
        </li>
      )}
    </ol>
  );
}
