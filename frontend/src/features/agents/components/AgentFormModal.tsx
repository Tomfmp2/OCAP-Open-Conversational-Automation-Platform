"use client";

import React from "react";
import Link from "next/link";
import { useChannels } from "@/features/channels/api/useChannelsData";
import { useKnowledgeBases } from "@/features/knowledge/api/useKnowledgeData";
import { Button, FormError, Input, Modal, Textarea } from "@/shared/components/ui";
import { useAgentTools, useDeleteAgent, useSaveAgent, type Agent } from "../api/useAgentsData";

interface AgentFormModalProps {
  open: boolean;
  /** Agente a editar; sin él, se crea uno nuevo. */
  agent?: Agent | null;
  onClose: () => void;
}

const PROMPT_EXAMPLE =
  "Eres el asistente de ventas de Panadería Sol. Respondes con amabilidad y en pocas líneas. " +
  "Das precios y horarios solo si están en el conocimiento.";

export function AgentFormModal({ open, agent, onClose }: AgentFormModalProps) {
  const [confirmDelete, setConfirmDelete] = React.useState(false);
  const close = () => {
    setConfirmDelete(false);
    onClose();
  };

  return (
    <Modal open={open} onClose={close} title={confirmDelete && agent ? `¿Eliminar ${agent.name}?` : agent ? `Editar ${agent.name}` : "Crear agente"} className="sm:max-w-xl">
      {open &&
        (confirmDelete && agent ? (
          <DeleteAgent agent={agent} onCancel={() => setConfirmDelete(false)} onDone={close} />
        ) : (
          // key: el formulario arranca limpio con los datos del agente cada vez que se abre
          <AgentForm key={agent?.id ?? "new"} agent={agent ?? null} onDone={close} onDelete={() => setConfirmDelete(true)} />
        ))}
    </Modal>
  );
}

function AgentForm({ agent, onDone, onDelete }: { agent: Agent | null; onDone: () => void; onDelete: () => void }) {
  const [name, setName] = React.useState(agent?.name ?? "");
  const [description, setDescription] = React.useState(agent?.description ?? "");
  const [systemPrompt, setSystemPrompt] = React.useState(agent?.systemPrompt ?? "");
  const [tools, setTools] = React.useState<string[]>(agent?.enabledTools ?? []);
  const [bases, setBases] = React.useState<string[]>(agent?.knowledgeBaseIds ?? []);
  const [remember, setRemember] = React.useState(agent?.rememberContacts ?? true);
  const save = useSaveAgent();
  const available = useAgentTools();
  const collections = useKnowledgeBases();
  const toggleBase = (id: string) => setBases((current) => (current.includes(id) ? current.filter((b) => b !== id) : [...current, id]));

  const toggleTool = (tool: string) => setTools((current) => (current.includes(tool) ? current.filter((t) => t !== tool) : [...current, tool]));

  return (
    <form
      className="space-y-5"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate(
          {
            id: agent?.id,
            draft: { name: name.trim(), description: description.trim(), systemPrompt: systemPrompt.trim(), allowedTools: tools, knowledgeBaseIds: bases, rememberContacts: remember },
          },
          { onSuccess: onDone }
        );
      }}
    >
      <Input label="Nombre" required maxLength={100} placeholder="Ventas" value={name} onChange={(e) => setName(e.target.value)} />
      <Input
        label="Para qué sirve"
        hint="Opcional. Te ayuda a reconocerlo en la lista."
        maxLength={500}
        placeholder="Consultas de productos y pedidos"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />
      <Textarea
        label="Instrucciones"
        hint="Cómo debe responder. Además usa el historial de la conversación y tu conocimiento."
        required
        rows={6}
        placeholder={PROMPT_EXAMPLE}
        value={systemPrompt}
        onChange={(e) => setSystemPrompt(e.target.value)}
      />

      <label className="flex items-start gap-3 rounded-md border border-neutral-200 p-3 text-sm has-[:checked]:border-neutral-950">
        <input type="checkbox" className="mt-0.5 h-4 w-4 accent-neutral-950" checked={remember} onChange={(e) => setRemember(e.target.checked)} />
        <span>
          <span className="block font-medium text-neutral-800">Recordar a los clientes</span>
          <span className="block text-xs text-neutral-600">Nombre, preferencias y lo que hablaron antes, durante el plazo de Ajustes.</span>
        </span>
      </label>

      {collections.data && collections.data.length > 1 && (
        <fieldset className="space-y-2">
          <legend className="text-sm font-medium text-neutral-800">Conocimiento que consulta</legend>
          <p className="text-xs text-neutral-600">Sin marcar ninguna, consulta todas las colecciones.</p>
          {collections.data.map((collection) => (
            <label key={collection.id} className="flex items-center gap-3 rounded-md border border-neutral-200 p-3 text-sm has-[:checked]:border-neutral-950">
              <input type="checkbox" className="h-4 w-4 accent-neutral-950" checked={bases.includes(collection.id)} onChange={() => toggleBase(collection.id)} />
              <span className="text-neutral-800">{collection.name}</span>
            </label>
          ))}
        </fieldset>
      )}

      {available.data && available.data.length > 0 && (
        <fieldset className="space-y-2">
          <legend className="text-sm font-medium text-neutral-800">Acciones que puede hacer</legend>
          <p className="text-xs text-neutral-600">Se ejecutan de verdad: activa solo las que necesita.</p>
          {available.data.map((tool) => (
            <label key={tool.name} className="flex items-start gap-3 rounded-md border border-neutral-200 p-3 text-sm has-[:checked]:border-neutral-950">
              <input type="checkbox" className="mt-0.5 h-4 w-4 accent-neutral-950" checked={tools.includes(tool.name)} onChange={() => toggleTool(tool.name)} />
              <span className="text-neutral-800">{tool.description}</span>
            </label>
          ))}
        </fieldset>
      )}

      <FormError error={save.error} />
      <div className="flex items-center justify-between gap-2 border-t border-neutral-200 pt-4">
        {agent ? (
          <Button variant="ghost" className="text-danger hover:bg-danger-subtle hover:text-danger" onClick={onDelete}>
            Eliminar
          </Button>
        ) : (
          <span />
        )}
        <div className="flex gap-2">
          <Button variant="ghost" onClick={onDone} disabled={save.isPending}>
            Cancelar
          </Button>
          <Button type="submit" loading={save.isPending}>
            {agent ? "Guardar cambios" : "Crear agente"}
          </Button>
        </div>
      </div>
    </form>
  );
}

function DeleteAgent({ agent, onCancel, onDone }: { agent: Agent; onCancel: () => void; onDone: () => void }) {
  const remove = useDeleteAgent();
  const channels = useChannels();
  const usedBy = channels.data?.filter((c) => c.agentId === agent.id) ?? [];

  return (
    <div className="space-y-5">
      {usedBy.length > 0 ? (
        <div role="alert" className="rounded-lg border border-warning/30 bg-warning-subtle p-4 text-sm">
          <p className="font-medium text-warning">Este agente está en uso</p>
          <p className="mt-1 text-neutral-700">
            Responde en: {usedBy.map((c) => c.name).join(", ")}. Asígnales otro agente antes de eliminarlo.
          </p>
          <Link href="/channels" className="mt-2 inline-block font-medium text-neutral-950 underline underline-offset-2">
            Ir a Canales
          </Link>
        </div>
      ) : (
        <p className="text-sm text-neutral-700">Se elimina con sus instrucciones. No se puede deshacer.</p>
      )}
      <FormError error={remove.error} />
      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button
          variant="danger"
          loading={remove.isPending}
          disabled={channels.isLoading || usedBy.length > 0}
          onClick={() => remove.mutate(agent.id, { onSuccess: onDone })}
        >
          Eliminar agente
        </Button>
      </div>
    </div>
  );
}
