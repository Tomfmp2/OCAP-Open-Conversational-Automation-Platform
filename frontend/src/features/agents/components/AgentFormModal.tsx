"use client";

import React from "react";
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
  const save = useSaveAgent();
  const available = useAgentTools();

  const toggleTool = (tool: string) => setTools((current) => (current.includes(tool) ? current.filter((t) => t !== tool) : [...current, tool]));

  return (
    <form
      className="space-y-5"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate(
          { id: agent?.id, draft: { name: name.trim(), description: description.trim(), systemPrompt: systemPrompt.trim(), allowedTools: tools } },
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
  return (
    <div className="space-y-5">
      <p className="text-sm text-neutral-700">Los canales que lo usan pasan a responder con el agente de atención.</p>
      <FormError error={remove.error} />
      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button variant="danger" loading={remove.isPending} onClick={() => remove.mutate(agent.id, { onSuccess: onDone })}>
          Eliminar agente
        </Button>
      </div>
    </div>
  );
}
