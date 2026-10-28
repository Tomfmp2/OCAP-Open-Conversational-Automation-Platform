"use client";

import Link from "next/link";
import { useAiProviderOptions, useAssignChannelAi } from "../api/useChannelsData";

interface ChannelAiSelectProps {
  connectionId: string;
  aiProviderConfigId: string | null;
}

/** Elige qué proveedor de IA (de «IA y modelos») usa el agente de este canal. */
export function ChannelAiSelect({ connectionId, aiProviderConfigId }: ChannelAiSelectProps) {
  const options = useAiProviderOptions();
  const assign = useAssignChannelAi();
  const id = `channel-ai-${connectionId}`;
  const enabled = options.data?.filter((o) => o.enabled || o.id === aiProviderConfigId) ?? [];

  return (
    <div className="space-y-1">
      <label htmlFor={id} className="block text-xs font-medium text-neutral-600">
        IA que usa
      </label>
      <select
        id={id}
        value={aiProviderConfigId ?? ""}
        disabled={options.isLoading || assign.isPending}
        onChange={(e) => assign.mutate({ connectionId, aiProviderConfigId: e.target.value || null })}
        className="focus-ring w-full rounded-md border border-neutral-300 bg-white px-2.5 py-1.5 text-xs text-neutral-900 disabled:opacity-60"
      >
        <option value="">La activa de la organización</option>
        {enabled.map((option) => (
          <option key={option.id} value={option.id} disabled={!option.enabled}>
            {option.label}
            {option.enabled ? "" : " (deshabilitada)"}
          </option>
        ))}
      </select>
      {options.data?.length === 0 && (
        <p className="text-xs text-neutral-500">
          Sin proveedores registrados:{" "}
          <Link href="/intelligence" className="underline underline-offset-2 hover:text-neutral-950">
            agrega uno en IA y modelos
          </Link>
          .
        </p>
      )}
      {assign.isError && (
        <p role="alert" className="text-xs text-neutral-950">
          {assign.error instanceof Error ? assign.error.message : "No se pudo asignar la IA."}
        </p>
      )}
    </div>
  );
}
