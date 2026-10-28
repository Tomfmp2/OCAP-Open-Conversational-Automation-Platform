"use client";

import React from "react";
import { AlertCircle } from "lucide-react";
import { Modal } from "@/shared/components/ui/Modal";
import { Input } from "@/shared/components/ui/Input";
import { Button } from "@/shared/components/ui/Button";
import { useAgentTools, useSaveAgent, type Agent } from "../api/useAgentsData";

interface AgentFormModalProps {
  open: boolean;
  /** Agente a editar; sin él, se crea uno nuevo. */
  agent?: Agent | null;
  onClose: () => void;
}

const PROMPT_EXAMPLE =
  "Eres el asistente de ventas de Panadería Sol. Respondes con amabilidad y en pocas líneas. " +
  "Informas precios y horarios solo si están en la base de conocimiento.";

export function AgentFormModal({ open, agent, onClose }: AgentFormModalProps) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      title={agent ? `Editar ${agent.name}` : "Crear agente"}
      description="Define cómo responde a tus clientes. Lo asignas a un canal en Canales."
      className="max-w-xl"
    >
      {/* key: el formulario arranca limpio con los datos del agente cada vez que se abre */}
      {open && <AgentForm key={agent?.id ?? "new"} agent={agent ?? null} onDone={onClose} />}
    </Modal>
  );
}

function AgentForm({ agent, onDone }: { agent: Agent | null; onDone: () => void }) {
  const [name, setName] = React.useState(agent?.name ?? "");
  const [description, setDescription] = React.useState(agent?.description ?? "");
  const [systemPrompt, setSystemPrompt] = React.useState(agent?.systemPrompt ?? "");
  const [tools, setTools] = React.useState<string[]>(agent?.enabledTools ?? []);
  const [error, setError] = React.useState<string | null>(null);
  const save = useSaveAgent();
  const available = useAgentTools();

  const toggleTool = (tool: string) =>
    setTools((current) => (current.includes(tool) ? current.filter((t) => t !== tool) : [...current, tool]));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await save.mutateAsync({
        id: agent?.id,
        draft: { name: name.trim(), description: description.trim(), systemPrompt: systemPrompt.trim(), allowedTools: tools },
      });
      onDone();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "No se pudo guardar el agente.");
    }
  };

  return (
    <form onSubmit={(e) => void submit(e)} className="space-y-4">
      {error && (
        <p role="alert" className="flex items-center gap-2 rounded-md border-2 border-neutral-950 p-3 text-xs text-neutral-950">
          <AlertCircle className="h-4 w-4 shrink-0" aria-hidden /> {error}
        </p>
      )}

      <Input
        id="agent-name"
        label="Nombre"
        required
        maxLength={100}
        placeholder="Ventas"
        value={name}
        onChange={(e) => setName(e.target.value)}
      />
      <Input
        id="agent-description"
        label="Descripción (opcional)"
        maxLength={500}
        placeholder="Atiende consultas de productos y pedidos"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />

      <div className="space-y-1.5">
        <label htmlFor="agent-prompt" className="block text-xs font-medium tracking-wide text-neutral-700">
          Instrucciones
        </label>
        <textarea
          id="agent-prompt"
          required
          rows={6}
          placeholder={PROMPT_EXAMPLE}
          value={systemPrompt}
          onChange={(e) => setSystemPrompt(e.target.value)}
          className="focus-ring w-full rounded-md border border-neutral-300 bg-white px-3.5 py-2.5 text-sm text-neutral-950 placeholder:text-neutral-500"
        />
        <p className="text-xs text-neutral-500">
          Además usa el historial de la conversación y tu base de conocimiento, y puede pasar la conversación a una persona.
        </p>
      </div>

      <fieldset className="space-y-2">
        <legend className="text-xs font-medium tracking-wide text-neutral-700">Herramientas</legend>
        {available.isLoading && <p className="text-xs text-neutral-500">Cargando herramientas…</p>}
        {available.data?.length === 0 && <p className="text-xs text-neutral-500">No hay herramientas disponibles.</p>}
        {available.data?.map((tool) => (
          <label key={tool.name} className="flex items-start gap-2 text-xs text-neutral-800">
            <input
              type="checkbox"
              className="focus-ring mt-0.5"
              checked={tools.includes(tool.name)}
              onChange={() => toggleTool(tool.name)}
            />
            <span>
              <span className="font-medium">{tool.name}</span>
              <span className="block text-neutral-500">{tool.description}</span>
            </span>
          </label>
        ))}
        <p className="text-xs text-neutral-500">
          Solo aparecen acciones seguras frente a clientes (agendar, registrar datos); actúan de verdad, así que habilita
          solo las necesarias. Leer o enviar correos del negocio no se ofrece a los agentes de canal.
        </p>
      </fieldset>

      <div className="flex justify-end gap-2 border-t border-neutral-200 pt-3">
        <Button type="button" variant="ghost" size="sm" onClick={onDone} disabled={save.isPending}>
          Cancelar
        </Button>
        <Button type="submit" size="sm" loading={save.isPending}>
          {agent ? "Guardar cambios" : "Crear agente"}
        </Button>
      </div>
    </form>
  );
}
