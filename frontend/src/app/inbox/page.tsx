"use client";

import React from "react";
import { ArrowLeft } from "lucide-react";
import { useInbox } from "@/features/inbox/api/useInbox";
import { ConversationList } from "@/features/inbox/components/ConversationList";
import { ConversationDetail } from "@/features/inbox/components/ConversationDetail";
import { EmptyState, ErrorState, ListSkeleton, Segmented } from "@/shared/components/ui";
import { cn } from "@/shared/utils/cn";

type Filter = "all" | "team";

/** Bandeja a pantalla completa: lista y conversación lado a lado; en móvil, una u otra. */
export default function InboxPage() {
  const [filter, setFilter] = React.useState<Filter>("all");
  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const inbox = useInbox(filter === "team");
  const selected = inbox.data?.find((c) => c.id === selectedId) ?? null;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <header className={cn("flex flex-wrap items-center justify-between gap-3 border-b border-neutral-200 bg-white px-4 py-3 sm:px-6", selected && "hidden md:flex")}>
        <h1 className="text-xl font-semibold tracking-tight text-neutral-950">Conversaciones</h1>
        <Segmented
          label="Mostrar"
          value={filter}
          onChange={setFilter}
          options={[
            { value: "all", label: "Todas" },
            { value: "team", label: "Atiende el equipo" },
          ]}
        />
      </header>

      {inbox.isError ? (
        <div className="p-4 sm:p-6">
          <ErrorState message={inbox.error instanceof Error ? inbox.error.message : undefined} onRetry={() => void inbox.refetch()} />
        </div>
      ) : inbox.isLoading ? (
        <div className="p-4 sm:p-6">
          <ListSkeleton label="Cargando conversaciones" />
        </div>
      ) : !inbox.data?.length ? (
        <div className="p-4 sm:p-6">
          <EmptyState
            title={filter === "team" ? "Ninguna conversación espera a tu equipo" : "Todavía no hay conversaciones"}
            description={
              filter === "team"
                ? "Aquí aparecen las que el agente pasa a una persona o las que tomas tú."
                : "Cuando un cliente escriba a uno de tus canales, la conversación aparecerá aquí."
            }
          />
        </div>
      ) : (
        <div className="grid min-h-0 flex-1 grid-cols-1 bg-white md:grid-cols-[20rem_minmax(0,1fr)]">
          <div className={cn("min-h-0 overflow-y-auto border-neutral-200 md:border-r", selected && "hidden md:block")}>
            <ConversationList conversations={inbox.data} selectedId={selectedId} onSelect={setSelectedId} />
          </div>
          <div className={cn("min-h-0", !selected && "hidden md:block")}>
            {selected ? (
              <ConversationDetail
                key={selected.id}
                conversation={selected}
                backButton={
                  <button
                    type="button"
                    onClick={() => setSelectedId(null)}
                    className="focus-ring -ml-2 rounded-md p-2 text-neutral-700 hover:bg-neutral-100 md:hidden"
                    aria-label="Volver a la lista"
                  >
                    <ArrowLeft className="h-4 w-4" aria-hidden />
                  </button>
                }
              />
            ) : (
              <p className="flex h-full items-center justify-center p-6 text-sm text-neutral-600">Elige una conversación de la lista.</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
