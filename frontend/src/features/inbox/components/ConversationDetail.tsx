"use client";

import React from "react";
import { Send } from "lucide-react";
import { MemoryPanel } from "@/features/memory/components/MemoryPanel";
import { Button, FormError, Modal } from "@/shared/components/ui";
import { cn } from "@/shared/utils/cn";
import { useInboxActions, useInboxMessages, type InboxConversation } from "../api/useInbox";
import { formatTime } from "./ConversationList";

const SENDER_LABEL: Record<string, string> = { Agent: "Agente", Operator: "Equipo", System: "Sistema" };

/** Mensajes de la conversación y acciones del equipo: tomar, responder y devolver al agente. */
export function ConversationDetail({ conversation, backButton }: { conversation: InboxConversation; backButton?: React.ReactNode }) {
  const messages = useInboxMessages(conversation.id);
  const { takeOver, release, reply } = useInboxActions(conversation.id);
  const [text, setText] = React.useState("");
  const [memoryOpen, setMemoryOpen] = React.useState(false);
  const bottomRef = React.useRef<HTMLDivElement>(null);
  const count = messages.data?.length ?? 0;

  React.useEffect(() => {
    bottomRef.current?.scrollIntoView({ block: "end" });
  }, [count]);

  const send = () => {
    const value = text.trim();
    if (value) reply.mutate(value, { onSuccess: () => setText("") });
  };

  return (
    <section className="flex h-full min-h-0 flex-col" aria-label={`Conversación con ${conversation.contactName}`}>
      <header className="flex items-center gap-3 border-b border-neutral-200 px-4 py-3">
        {backButton}
        <div className="min-w-0 flex-1">
          <h2 className="truncate text-sm font-semibold text-neutral-950">{conversation.contactName}</h2>
          <p className="truncate text-xs text-neutral-600">
            {conversation.channel}
            {conversation.externalContactId && `, ${conversation.externalContactId}`}
          </p>
          {conversation.handledByHuman && (
            <p className="text-xs text-warning">Atiendes tú: el agente vuelve a responder si nadie del equipo escribe en 15 minutos.</p>
          )}
        </div>
        {conversation.handledByHuman ? (
          <Button variant="secondary" size="sm" onClick={() => release.mutate()} loading={release.isPending}>
            Devolver al agente
          </Button>
        ) : (
          <Button variant="secondary" size="sm" onClick={() => takeOver.mutate()} loading={takeOver.isPending}>
            Tomar conversación
          </Button>
        )}
        <Button variant="ghost" size="sm" onClick={() => setMemoryOpen(true)}>
          Memoria
        </Button>
      </header>
      <Modal open={memoryOpen} onClose={() => setMemoryOpen(false)} title={`Lo que se recuerda de ${conversation.contactName}`} className="sm:max-w-lg">
        <MemoryPanel
          basePath={`/api/inbox/${conversation.id}/memory`}
          open={memoryOpen}
          editable
          emptyText="Los agentes guardan lo que conviene recordar cuando una conversación lleva 10 minutos sin mensajes."
        />
      </Modal>

      <div className="min-h-0 flex-1 space-y-3 overflow-y-auto bg-neutral-50 px-4 py-4" aria-live="polite">
        {messages.isLoading && <p className="text-sm text-neutral-600">Cargando mensajes…</p>}
        <FormError error={messages.error} />
        {messages.data?.map((message) => {
          const fromContact = message.sender === "User";
          return (
            <div key={message.id} className={cn("flex flex-col gap-1", fromContact ? "items-start" : "items-end")}>
              <p
                className={cn(
                  "max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-3.5 py-2 text-sm",
                  fromContact && "rounded-bl-md border border-neutral-200 bg-white text-neutral-900",
                  message.sender === "Agent" && "rounded-br-md bg-neutral-200 text-neutral-900",
                  message.sender === "Operator" && "rounded-br-md bg-neutral-950 text-white",
                  message.sender === "System" && "bg-transparent text-xs text-neutral-600"
                )}
              >
                {message.text}
              </p>
              <span className="px-1 text-xs text-neutral-600">
                {SENDER_LABEL[message.sender] && `${SENDER_LABEL[message.sender]}, `}
                {formatTime(message.createdAt)}
              </span>
            </div>
          );
        })}
        <div ref={bottomRef} />
      </div>

      <footer className="space-y-2 border-t border-neutral-200 bg-white p-3">
        <p className="text-xs text-neutral-600">
          {conversation.handledByHuman
            ? "Atiende tu equipo: el agente no responde hasta que se la devuelvas."
            : "Responde el agente. Si escribes, la conversación pasa a tu equipo."}
        </p>
        {conversation.canReply ? (
          <form
            className="flex items-end gap-2"
            onSubmit={(e) => {
              e.preventDefault();
              send();
            }}
          >
            <label htmlFor="inbox-reply" className="sr-only">
              Respuesta
            </label>
            <textarea
              id="inbox-reply"
              rows={2}
              value={text}
              onChange={(e) => setText(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter" && !e.shiftKey) {
                  e.preventDefault();
                  send();
                }
              }}
              placeholder={`Escribe a ${conversation.contactName}`}
              className="focus-ring min-w-0 flex-1 resize-none rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm"
            />
            <Button type="submit" disabled={!text.trim()} loading={reply.isPending} aria-label="Enviar">
              <Send className="h-4 w-4" aria-hidden />
            </Button>
          </form>
        ) : (
          <p className="text-sm text-neutral-600">Este canal no permite responder desde el panel.</p>
        )}
        <FormError error={takeOver.error ?? release.error ?? reply.error} />
      </footer>
    </section>
  );
}
