"use client";

import React from "react";
import { CheckCircle2 } from "lucide-react";
import { useAgentTools } from "@/features/agents/api/useAgentsData";
import { useKnowledgeBases } from "@/features/knowledge/api/useKnowledgeData";
import { Button, FormError } from "@/shared/components/ui";
import { useChannelCapabilities, type ChannelConnection } from "../api/useChannelsData";

/**
 * Lo que el agente puede usar en este canal. El mismo agente puede responder con otros documentos y otras acciones
 * en otro canal: las instrucciones son del agente, las capacidades del canal.
 */
export function ChannelCapabilitiesForm({ channel }: { channel: ChannelConnection }) {
  const collections = useKnowledgeBases();
  const tools = useAgentTools();
  const save = useChannelCapabilities();
  const [bases, setBases] = React.useState<string[]>(channel.knowledgeBaseIds ?? []);
  const [actions, setActions] = React.useState<string[]>(channel.tools ?? []);
  const toggle = (list: string[], value: string) => (list.includes(value) ? list.filter((v) => v !== value) : [...list, value]);

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate({ id: channel.id, knowledgeBaseIds: bases, tools: actions });
      }}
    >
      <fieldset className="space-y-2">
        <legend className="text-sm font-medium text-neutral-800">Conocimiento</legend>
        <p className="text-xs text-neutral-600">Documentos con los que responde aquí. Sin marcar ninguno, usa todos.</p>
        {collections.isLoading ? (
          <p className="text-sm text-neutral-600">Cargando colecciones…</p>
        ) : !collections.data?.length ? (
          <p className="text-sm text-neutral-600">Todavía no hay colecciones en Conocimiento.</p>
        ) : (
          collections.data.map((collection) => (
            <label key={collection.id} className="flex items-center gap-3 rounded-md border border-neutral-200 p-3 text-sm has-[:checked]:border-neutral-950">
              <input type="checkbox" className="h-4 w-4 accent-neutral-950" checked={bases.includes(collection.id)} onChange={() => setBases(toggle(bases, collection.id))} />
              <span className="min-w-0 flex-1 text-neutral-800">{collection.name}</span>
              <span className="text-xs text-neutral-600">{collection.documentCount} doc.</span>
            </label>
          ))
        )}
      </fieldset>

      {!!tools.data?.length && (
        <fieldset className="space-y-2">
          <legend className="text-sm font-medium text-neutral-800">Acciones</legend>
          <p className="text-xs text-neutral-600">Se ejecutan de verdad cuando el cliente lo pide: activa solo las que este canal necesita.</p>
          {tools.data.map((tool) => (
            <label key={tool.name} className="flex items-start gap-3 rounded-md border border-neutral-200 p-3 text-sm has-[:checked]:border-neutral-950">
              <input type="checkbox" className="mt-0.5 h-4 w-4 accent-neutral-950" checked={actions.includes(tool.name)} onChange={() => setActions(toggle(actions, tool.name))} />
              <span className="text-neutral-800">{tool.description}</span>
            </label>
          ))}
        </fieldset>
      )}

      {channel.tools === null && (
        <p className="text-xs text-neutral-600">Mientras no guardes, este canal usa lo que tenía configurado el agente.</p>
      )}
      <FormError error={save.error} />
      {save.isSuccess && (
        <p role="status" className="flex items-center gap-2 text-sm text-success">
          <CheckCircle2 className="h-4 w-4" aria-hidden /> Conocimiento y acciones guardados.
        </p>
      )}
      <Button type="submit" variant="secondary" loading={save.isPending}>
        Guardar conocimiento y acciones
      </Button>
    </form>
  );
}
