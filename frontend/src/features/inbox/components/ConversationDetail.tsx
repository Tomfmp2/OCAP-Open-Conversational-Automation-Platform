"use client";

import React from "react";
import { AlertCircle, Bot, Loader2, Send, UserRound } from "lucide-react";
import { Button } from "@/shared/components/ui/Button";
import { Badge } from "@/shared/components/ui/Badge";
import { cn } from "@/shared/utils/cn";
import { useInboxActions, useInboxMessages, type InboxConversation } from "../api/useInbox";

interface ConversationDetailProps {
  conversation: InboxConversation;
}

const SENDER_LABEL: Record<string, string> = { User: "", Agent: "Agente", Operator: "Equipo", System: "Sistema" };

/** Mensajes de la conversación y acciones del equipo: tomar, responder y devolver al agente. */
export function ConversationDetail({ conversation }: ConversationDetailProps) {
  const messages = useInboxMessages(conversation.id);
  const { takeOver, release, reply } = useInboxActions(conversation.id);
  const [text, setText] = React.useState("");
  const bottomRef = React.useRef<HTMLDivElement>(null);

  const count = messages.data?.length ?? 0;
  React.useEffect(() => {
    bottomRef.current?.scrollIntoView({ block: "end" });
  }, [count]);

  const error = [takeOver, release, reply].find((m) => m.isError)?.error;

  const send = async () => {
    const value = text.trim();
    if (!value) return;
    try {
      await reply.mutateAsync(value);
      setText("");
    } catch {
      // el error se muestra debajo del cuadro de texto
    }
  };

  return (
    <section className="flex h-full min-h-0 flex-col" aria-label={`Conversación con ${conversation.contactName}`}>
      <header className="flex flex-wrap items-center justify-between gap-2 border-b border-neutral-200 px-4 py-3">
        <div className="min-w-0">
          <h2 className="truncate text-sm font-semibold text-neutral-950">{conversation.contactName}</h2>
          <p className="text-[11px] text-neutral-500">
            {conversation.channel}
            {conversation.externalContactId ? ` · ${conversation.externalContactId}` : ""}
          </p>
        </div>
        {conversation.handledByHuman ? (
          <Button variant="secondary" size="sm" onClick={() => release.mutate()} loading={release.isPending}>
            <Bot className="h-3.5 w-3.5" aria-hidden /> Devolver al agente
          </Button>
        ) : (
          <Button variant="secondary" size="sm" onClick={() => takeOver.mutate()} loading={takeOver.isPending}>
            <UserRound className="h-3.5 w-3.5" aria-hidden /> Tomar conversación
          </Button>
        )}
      </header>

      <div className="min-h-0 flex-1 space-y-2 overflow-y-auto bg-neutral-50 px-4 py-4">
        {messages.isLoading && (
          <p className="flex items-center gap-2 text-xs text-neutral-500">
            <Loader2 className="h-3.5 w-3.5 animate-spin motion-reduce:animate-none" aria-hidden /> Cargando mensajes…
          </p>
        )}
        {messages.isError && <p className="text-xs text-neutral-950">No se pudieron cargar los mensajes.</p>}
        {messages.data?.map((message) => {
          const fromContact = message.sender === "User";
          return (
            <div key={message.id} className={cn("flex flex-col gap-0.5", fromContact ? "items-start" : "items-end")}>
              {!fromContact && <span className="text-[10px] text-neutral-500">{SENDER_LABEL[message.sender]}</span>}
              <p
                className={cn(
                  "max-w-[85%] whitespace-pre-wrap rounded-2xl px-3 py-2 text-sm",
                  fromContact && "border border-neutral-200 bg-white text-neutral-900",
                  message.sender === "Agent" && "bg-neutral-200 text-neutral-900",
                  message.sender === "Operator" && "bg-neutral-950 text-white"
                )}
              >
                {message.text}
              </p>
            </div>
          );
        })}
        <div ref={bottomRef} />
      </div>

      <footer className="space-y-2 border-t border-neutral-200 p-3">
        {conversation.handledByHuman ? (
          <Badge tone="warning">Atiende tu equipo: el agente no responde en esta conversación</Badge>
        ) : (
          <p className="text-[11px] text-neutral-500">Responde el agente. Si escribes, tomas la conversación.</p>
        )}
        {conversation.canReply ? (
          <form
            className="flex gap-2"
            onSubmit={(e) => {
              e.preventDefault();
              void send();
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
                  void send();
                }
              }}
              placeholder={`Responder a ${conversation.contactName}…`}
              className="focus-ring w-full resize-none rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm"
            />
            <Button type="submit" size="sm" disabled={!text.trim()} loading={reply.isPending} aria-label="Enviar respuesta">
              <Send className="h-4 w-4" aria-hidden />
            </Button>
          </form>
        ) : (
          <p className="text-xs text-neutral-500">Este canal no permite responder desde el panel.</p>
        )}
        {error && (
          <p role="alert" className="flex items-center gap-2 text-xs text-neutral-950">
            <AlertCircle className="h-3.5 w-3.5" aria-hidden /> {error instanceof Error ? error.message : "No se pudo completar la acción."}
          </p>
        )}
      </footer>
    </section>
  );
}
