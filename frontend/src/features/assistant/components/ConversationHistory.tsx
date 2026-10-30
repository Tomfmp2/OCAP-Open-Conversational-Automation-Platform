"use client";

import React from "react";
import { Pencil, Trash2 } from "lucide-react";
import {
  Button,
  EmptyState,
  ErrorState,
  FormError,
  Input,
  ListSkeleton,
  Modal,
  Segmented,
} from "@/shared/components/ui";
import { MemoryPanel } from "@/features/memory/components/MemoryPanel";
import {
  useAssistantConversationActions,
  useAssistantConversations,
  type AssistantConversation,
} from "../api/usePrincipalAgentChat";

const dateFormat = new Intl.DateTimeFormat("es", {
  dateStyle: "medium",
  timeStyle: "short",
});

interface ConversationHistoryProps {
  open: boolean;
  onClose: () => void;
  currentId: string | null;
  onOpen: (id: string) => void;
  /** La conversación abierta se borró: el chat vuelve a empezar. */
  onDeletedCurrent: () => void;
}

export function ConversationHistory({
  open,
  onClose,
  currentId,
  onOpen,
  onDeletedCurrent,
}: ConversationHistoryProps) {
  const [tab, setTab] = React.useState<"conversations" | "memory">("conversations");
  const conversations = useAssistantConversations(open && tab === "conversations");

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Conversaciones"
      description="Solo tú las ves."
      className="sm:max-w-lg"
    >
      <div className="mb-4">
        <Segmented
          label="Ver"
          value={tab}
          onChange={setTab}
          options={[
            { value: "conversations", label: "Conversaciones" },
            { value: "memory", label: "Lo que recuerdo de ti" },
          ]}
        />
      </div>
      {tab === "memory" ? (
        <MemoryPanel
          basePath="/api/agents/principal/memory"
          open={open}
          emptyText="El asistente guarda lo que conviene recordar de ti cuando una conversación lleva 10 minutos sin mensajes."
        />
      ) : conversations.isLoading ? (
        <ListSkeleton label="Cargando conversaciones" />
      ) : conversations.isError ? (
        <ErrorState
          message={
            conversations.error instanceof Error
              ? conversations.error.message
              : undefined
          }
          onRetry={() => void conversations.refetch()}
        />
      ) : !conversations.data?.length ? (
        <EmptyState
          title="Sin conversaciones"
          description="Lo que hables con el asistente se guarda aquí para retomarlo."
        />
      ) : (
        <ul className="-mx-1 max-h-[60vh] divide-y divide-neutral-200 overflow-y-auto">
          {conversations.data.map((c) => (
            <ConversationItem
              key={c.id}
              conversation={c}
              current={c.id === currentId}
              onOpen={() => {
                onOpen(c.id);
                onClose();
              }}
              onDeleted={() => c.id === currentId && onDeletedCurrent()}
            />
          ))}
        </ul>
      )}
    </Modal>
  );
}

function ConversationItem({
  conversation,
  current,
  onOpen,
  onDeleted,
}: {
  conversation: AssistantConversation;
  current: boolean;
  onOpen: () => void;
  onDeleted: () => void;
}) {
  const { rename, remove } = useAssistantConversationActions();
  const [mode, setMode] = React.useState<"view" | "rename" | "delete">("view");
  const [title, setTitle] = React.useState(conversation.title);

  if (mode === "rename") {
    return (
      <li className="px-1 py-3">
        <form
          className="space-y-3"
          onSubmit={(e) => {
            e.preventDefault();
            rename.mutate(
              { id: conversation.id, title: title.trim() },
              { onSuccess: () => setMode("view") },
            );
          }}
        >
          <Input
            label="Título"
            required
            maxLength={120}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            autoFocus
          />
          <FormError error={rename.error} />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" size="sm" onClick={() => setMode("view")}>
              Cancelar
            </Button>
            <Button type="submit" size="sm" loading={rename.isPending}>
              Guardar
            </Button>
          </div>
        </form>
      </li>
    );
  }

  if (mode === "delete") {
    return (
      <li className="space-y-3 px-1 py-3">
        <p className="text-sm text-neutral-800">
          ¿Eliminar «{conversation.title}»? No se puede deshacer.
        </p>
        <FormError error={remove.error} />
        <div className="flex justify-end gap-2">
          <Button variant="ghost" size="sm" onClick={() => setMode("view")}>
            Cancelar
          </Button>
          <Button
            variant="danger"
            size="sm"
            loading={remove.isPending}
            onClick={() =>
              remove.mutate(conversation.id, { onSuccess: onDeleted })
            }
          >
            Eliminar
          </Button>
        </div>
      </li>
    );
  }

  return (
    <li className="flex items-center gap-1">
      <button
        type="button"
        onClick={onOpen}
        aria-current={current || undefined}
        className="focus-ring min-w-0 flex-1 rounded-md px-1 py-3 text-left hover:bg-neutral-50"
      >
        <span className="block truncate text-sm font-medium text-neutral-950">
          {conversation.title}
        </span>
        <span className="block text-xs text-neutral-600">
          {current ? "Abierta, " : ""}
          {dateFormat.format(new Date(conversation.updatedAt))}
        </span>
      </button>
      <Button
        variant="ghost"
        size="sm"
        onClick={() => setMode("rename")}
        aria-label={`Renombrar ${conversation.title}`}
      >
        <Pencil className="h-4 w-4" aria-hidden />
      </Button>
      <Button
        variant="ghost"
        size="sm"
        onClick={() => setMode("delete")}
        aria-label={`Eliminar ${conversation.title}`}
      >
        <Trash2 className="h-4 w-4" aria-hidden />
      </Button>
    </li>
  );
}
