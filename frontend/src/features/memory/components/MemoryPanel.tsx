"use client";

import React from "react";
import { Pencil, Trash2 } from "lucide-react";
import { Button, EmptyState, ErrorState, FormError, Input, ListSkeleton } from "@/shared/components/ui";
import { useMemory, useMemoryActions, type MemoryItem } from "../api/useMemory";

const dateFormat = new Intl.DateTimeFormat("es", { dateStyle: "medium" });

interface MemoryPanelProps {
  basePath: string;
  open: boolean;
  /** Solo la memoria de los contactos se corrige a mano; la propia se borra. */
  editable?: boolean;
  emptyText: string;
}

/** Lo que recuerdan los agentes de una persona: datos y conversaciones anteriores, con su vencimiento. */
export function MemoryPanel({ basePath, open, editable = false, emptyText }: MemoryPanelProps) {
  const memory = useMemory(basePath, open);
  const { forget } = useMemoryActions(basePath);
  const [confirmAll, setConfirmAll] = React.useState(false);

  if (memory.isLoading) return <ListSkeleton label="Cargando memoria" rows={2} />;
  if (memory.isError) return <ErrorState onRetry={() => void memory.refetch()} />;
  if (!memory.data?.length) return <EmptyState title="Sin recuerdos" description={emptyText} />;

  const facts = memory.data.filter((m) => m.kind === "Fact");
  const episodes = memory.data.filter((m) => m.kind === "Episode");

  return (
    <div className="space-y-5">
      {facts.length > 0 && (
        <section className="space-y-2" aria-labelledby="memory-facts">
          <h3 id="memory-facts" className="text-sm font-semibold text-neutral-950">
            Datos
          </h3>
          <ul className="divide-y divide-neutral-200 rounded-lg border border-neutral-200">
            {facts.map((item) => (
              <MemoryRow key={item.id} item={item} basePath={basePath} editable={editable} />
            ))}
          </ul>
        </section>
      )}
      {episodes.length > 0 && (
        <section className="space-y-2" aria-labelledby="memory-episodes">
          <h3 id="memory-episodes" className="text-sm font-semibold text-neutral-950">
            Conversaciones anteriores
          </h3>
          <ul className="divide-y divide-neutral-200 rounded-lg border border-neutral-200">
            {episodes.map((item) => (
              <MemoryRow key={item.id} item={item} basePath={basePath} editable={editable} />
            ))}
          </ul>
        </section>
      )}

      <FormError error={forget.error} />
      {confirmAll ? (
        <div className="flex flex-wrap items-center justify-end gap-2">
          <p className="min-w-0 flex-1 text-sm text-neutral-800">¿Borrar todo lo que se recuerda? No se puede deshacer.</p>
          <Button variant="ghost" size="sm" onClick={() => setConfirmAll(false)}>
            Cancelar
          </Button>
          <Button variant="danger" size="sm" loading={forget.isPending} onClick={() => forget.mutate(undefined, { onSuccess: () => setConfirmAll(false) })}>
            Borrar todo
          </Button>
        </div>
      ) : (
        <div className="flex justify-end">
          <Button variant="ghost" size="sm" className="text-danger hover:bg-danger-subtle hover:text-danger" onClick={() => setConfirmAll(true)}>
            Borrar todo
          </Button>
        </div>
      )}
    </div>
  );
}

function MemoryRow({ item, basePath, editable }: { item: MemoryItem; basePath: string; editable: boolean }) {
  const { edit, forget } = useMemoryActions(basePath);
  const [editing, setEditing] = React.useState(false);
  const [text, setText] = React.useState(item.text);

  if (editing) {
    return (
      <li className="px-3 py-3">
        <form
          className="space-y-2"
          onSubmit={(e) => {
            e.preventDefault();
            edit.mutate({ id: item.id, text: text.trim() }, { onSuccess: () => setEditing(false) });
          }}
        >
          <Input label="Recuerdo" required maxLength={500} value={text} onChange={(e) => setText(e.target.value)} autoFocus />
          <FormError error={edit.error} />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" size="sm" onClick={() => setEditing(false)}>
              Cancelar
            </Button>
            <Button type="submit" size="sm" loading={edit.isPending}>
              Guardar
            </Button>
          </div>
        </form>
      </li>
    );
  }

  return (
    <li className="flex items-start gap-2 px-3 py-2.5">
      <div className="min-w-0 flex-1">
        <p className="text-sm text-neutral-900">{item.text}</p>
        <p className="text-xs text-neutral-600">Se olvida el {dateFormat.format(new Date(item.expiresAt))}</p>
      </div>
      {editable && (
        <Button variant="ghost" size="sm" onClick={() => setEditing(true)} aria-label={`Corregir: ${item.text}`}>
          <Pencil className="h-4 w-4" aria-hidden />
        </Button>
      )}
      <Button variant="ghost" size="sm" loading={forget.isPending} onClick={() => forget.mutate(item.id)} aria-label={`Borrar: ${item.text}`}>
        <Trash2 className="h-4 w-4" aria-hidden />
      </Button>
    </li>
  );
}
