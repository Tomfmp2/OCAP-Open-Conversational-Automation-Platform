"use client";

import React from "react";
import { ArrowLeft, Inbox } from "lucide-react";
import { useInbox } from "@/features/inbox/api/useInbox";
import { ConversationList } from "@/features/inbox/components/ConversationList";
import { ConversationDetail } from "@/features/inbox/components/ConversationDetail";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { EmptyState } from "@/shared/components/ui/EmptyState";
import { ErrorState } from "@/shared/components/ui/ErrorState";
import { Button } from "@/shared/components/ui/Button";
import { cn } from "@/shared/utils/cn";

export default function InboxPage() {
  const [onlyHuman, setOnlyHuman] = React.useState(false);
  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const inbox = useInbox(onlyHuman);
  const selected = inbox.data?.find((c) => c.id === selectedId) ?? null;

  return (
    <div className="mx-auto flex h-full max-w-7xl flex-col gap-4">
      <PageHeader
        title="Conversaciones"
        description="Lo que escriben tus clientes por WhatsApp y Telegram. Toma una conversación para responder tú."
        icon={<Inbox className="h-5 w-5 text-neutral-700" />}
      />

      <div className="flex gap-2" role="group" aria-label="Filtro">
        <Button size="sm" variant={onlyHuman ? "secondary" : "primary"} aria-pressed={!onlyHuman} onClick={() => setOnlyHuman(false)}>
          Todas
        </Button>
        <Button size="sm" variant={onlyHuman ? "primary" : "secondary"} aria-pressed={onlyHuman} onClick={() => setOnlyHuman(true)}>
          Atiende el equipo
        </Button>
      </div>

      {inbox.isError ? (
        <ErrorState message={inbox.error instanceof Error ? inbox.error.message : undefined} onRetry={() => void inbox.refetch()} />
      ) : inbox.isLoading ? (
        <p className="text-sm text-neutral-600">Cargando conversaciones…</p>
      ) : !inbox.data || inbox.data.length === 0 ? (
        <EmptyState
          title={onlyHuman ? "Ninguna conversación espera a tu equipo" : "Todavía no hay conversaciones"}
          description="Cuando un cliente escriba a un canal conectado, aparecerá aquí."
        />
      ) : (
        <div className="grid min-h-[32rem] flex-1 grid-cols-1 overflow-hidden rounded-md border border-neutral-200 bg-white md:grid-cols-[20rem_1fr]">
          <div className={cn("min-h-0 overflow-y-auto border-neutral-200 md:border-r", selected && "hidden md:block")}>
            <ConversationList conversations={inbox.data} selectedId={selectedId} onSelect={setSelectedId} />
          </div>
          <div className={cn("min-h-0", !selected && "hidden md:block")}>
            {selected ? (
              <div className="flex h-full flex-col">
                <button
                  type="button"
                  onClick={() => setSelectedId(null)}
                  className="focus-ring flex items-center gap-1 px-4 pt-3 text-xs text-neutral-600 md:hidden"
                >
                  <ArrowLeft className="h-3.5 w-3.5" aria-hidden /> Conversaciones
                </button>
                <ConversationDetail key={selected.id} conversation={selected} />
              </div>
            ) : (
              <p className="flex h-full items-center justify-center p-6 text-sm text-neutral-600">Elige una conversación.</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
