"use client";

import React from "react";
import { AlertCircle, Loader2 } from "lucide-react";
import {
  PROVIDER_MODEL_PRESETS,
  useIntelligenceData,
  type TenantProviderConfig,
} from "../api/useIntelligenceData";
import { Modal } from "@/shared/components/ui/Modal";
import { Input } from "@/shared/components/ui/Input";
import { Button } from "@/shared/components/ui/Button";

const PROVIDER_TYPES = ["Gemini", "OpenAI", "Claude", "Ollama"] as const;

interface ProviderFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Si se pasa, modo edición. */
  editing?: TenantProviderConfig | null;
  suggestedModels?: Record<string, string[]>;
  /** Prefill al crear (p. ej. desde catálogo runtime). */
  createPrefill?: { providerName: string; modelName?: string } | null;
}

export function ProviderFormModal({
  open,
  onClose,
  editing = null,
  suggestedModels = {},
  createPrefill = null,
}: ProviderFormModalProps) {
  const { createProviderMutation, updateProviderMutation } = useIntelligenceData();
  const isEdit = Boolean(editing);

  const [providerName, setProviderName] = React.useState<string>("Gemini");
  const [displayName, setDisplayName] = React.useState("");
  const [apiKey, setApiKey] = React.useState("");
  const [modelName, setModelName] = React.useState("gemini-3.5-flash");
  const [baseUrl, setBaseUrl] = React.useState("");
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (!open) return;
    setErrorMessage(null);
    if (editing) {
      setProviderName(editing.providerName);
      setDisplayName(editing.displayName);
      setModelName(editing.modelName);
      setBaseUrl(editing.baseUrl ?? "");
      setApiKey("");
      return;
    }
    const name = createPrefill?.providerName || "Gemini";
    setProviderName(name);
    setDisplayName(name);
    setModelName(
      createPrefill?.modelName ||
        PROVIDER_MODEL_PRESETS[name]?.[0] ||
        "gemini-3.5-flash"
    );
    setBaseUrl("");
    setApiKey("");
  }, [open, editing, createPrefill]);

  const presets = [
    ...(suggestedModels[providerName] ?? []),
    ...(PROVIDER_MODEL_PRESETS[providerName] ?? []),
  ].filter((v, i, arr) => arr.indexOf(v) === i);

  const pending =
    createProviderMutation.isPending || updateProviderMutation.isPending;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    try {
      if (isEdit && editing) {
        await updateProviderMutation.mutateAsync({
          id: editing.id,
          modelName: modelName.trim(),
          apiKey: apiKey.trim() || null,
          baseUrl: baseUrl.trim() || null,
        });
      } else {
        if (providerName !== "Ollama" && !apiKey.trim()) {
          setErrorMessage("API key requerida (salvo Ollama).");
          return;
        }
        await createProviderMutation.mutateAsync({
          providerName,
          displayName: displayName.trim() || providerName,
          modelName: modelName.trim(),
          apiKey: apiKey.trim(),
          baseUrl: baseUrl.trim() || null,
        });
      }
      onClose();
    } catch (err: unknown) {
      setErrorMessage(
        err instanceof Error ? err.message : "No se pudo guardar el proveedor"
      );
    }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={isEdit ? "Editar proveedor" : "Registrar proveedor"}
      description={
        isEdit
          ? "Cambia modelo, API key o Base URL. Deja la key vacía para conservar la del vault."
          : "Se guarda la API key cifrada en el vault del tenant."
      }
    >
      <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
        {errorMessage && (
          <div className="flex items-center gap-2 rounded-md border-2 border-neutral-950 bg-white px-3 py-2 text-xs text-neutral-950">
            <AlertCircle className="h-4 w-4 shrink-0" />
            <span>{errorMessage}</span>
          </div>
        )}

        {!isEdit && (
          <div className="space-y-2">
            <p className="text-xs font-medium text-neutral-700">Proveedor</p>
            <div className="grid grid-cols-4 gap-2">
              {PROVIDER_TYPES.map((type) => (
                <button
                  key={type}
                  type="button"
                  onClick={() => {
                    setProviderName(type);
                    setDisplayName(type);
                    const first =
                      PROVIDER_MODEL_PRESETS[type]?.[0] ?? modelName;
                    setModelName(first);
                  }}
                  className={`rounded-md border px-2 py-2 text-xs font-semibold transition ${
                    providerName === type
                      ? "border-neutral-950 bg-neutral-950 text-white"
                      : "border-neutral-200 bg-white text-neutral-600 hover:border-neutral-400"
                  }`}
                >
                  {type}
                </button>
              ))}
            </div>
          </div>
        )}

        <Input
          label="Nombre visible"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          disabled={isEdit}
          hint={isEdit ? "El tipo de proveedor no se puede cambiar." : undefined}
        />

        <Input
          label={isEdit ? "API key (opcional)" : "API key"}
          type="password"
          value={apiKey}
          onChange={(e) => setApiKey(e.target.value)}
          placeholder={isEdit ? "••••••••  (dejar vacío = no cambiar)" : "sk-… / AIza…"}
          required={!isEdit && providerName !== "Ollama"}
          hint={
            providerName === "Ollama"
              ? "Opcional para Ollama."
              : isEdit
                ? "Solo si quieres rotar la clave."
                : undefined
          }
        />

        <div className="space-y-2">
          <Input
            label="Modelo"
            value={modelName}
            onChange={(e) => setModelName(e.target.value)}
            required
          />
          {presets.length > 0 && (
            <div className="flex flex-wrap gap-1.5">
              {presets.slice(0, 6).map((m) => (
                <button
                  key={m}
                  type="button"
                  onClick={() => setModelName(m)}
                  className={`rounded border px-2 py-1 font-mono text-[10px] ${
                    modelName === m
                      ? "border-neutral-950 bg-neutral-100 text-neutral-950"
                      : "border-neutral-200 text-neutral-500 hover:border-neutral-400"
                  }`}
                >
                  {m}
                </button>
              ))}
            </div>
          )}
        </div>

        <Input
          label="Base URL (opcional)"
          value={baseUrl}
          onChange={(e) => setBaseUrl(e.target.value)}
          placeholder={
            providerName === "Ollama"
              ? "http://localhost:11434"
              : "https://…"
          }
          hint="Útil para Ollama o proxies compatibles."
        />

        <div className="flex justify-end gap-2 border-t border-neutral-100 pt-4">
          <Button type="button" variant="secondary" size="sm" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" size="sm" loading={pending}>
            {pending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
            {isEdit ? "Guardar cambios" : "Guardar en vault"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

/** Compatibilidad con imports antiguos. */
export function AddProviderModal({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  return <ProviderFormModal open={open} onClose={onClose} />;
}
