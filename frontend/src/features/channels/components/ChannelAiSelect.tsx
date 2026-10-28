"use client";

import Link from "next/link";
import { FormError, Select } from "@/shared/components/ui";
import { useAiProviderOptions, useAssignChannelAi } from "../api/useChannelsData";

/** Qué modelo de IA usa el agente de este canal. */
export function ChannelAiSelect({ connectionId, aiProviderConfigId }: { connectionId: string; aiProviderConfigId: string | null }) {
  const options = useAiProviderOptions();
  const assign = useAssignChannelAi();
  const available = options.data?.filter((o) => o.enabled || o.id === aiProviderConfigId) ?? [];

  return (
    <div className="space-y-2">
      <Select
        label="Modelo de IA"
        value={aiProviderConfigId ?? ""}
        disabled={options.isLoading || assign.isPending}
        onChange={(e) => assign.mutate({ connectionId, aiProviderConfigId: e.target.value || null })}
      >
        <option value="">El activo de la organización</option>
        {available.map((option) => (
          <option key={option.id} value={option.id} disabled={!option.enabled}>
            {option.label}
            {option.enabled ? "" : " (desactivado)"}
          </option>
        ))}
      </Select>
      {options.data?.length === 0 && (
        <p className="text-xs text-neutral-600">
          Aún no agregaste ninguno.{" "}
          <Link href="/intelligence" className="font-medium text-neutral-950 underline underline-offset-2">
            Agregar un modelo
          </Link>
        </p>
      )}
      <FormError error={assign.error} />
    </div>
  );
}
