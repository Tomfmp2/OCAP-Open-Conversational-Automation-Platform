"use client";

import React from "react";
import { X, Cpu, Lock, AlertCircle, Loader2 } from "lucide-react";
import { useIntelligenceData } from "../api/useIntelligenceData";

interface AddProviderModalProps {
  open: boolean;
  onClose: () => void;
}

export function AddProviderModal({ open, onClose }: AddProviderModalProps) {
  const { createProviderMutation } = useIntelligenceData();
  const [providerType, setProviderType] = React.useState<"OpenAI" | "Gemini" | "Ollama" | "Local">("OpenAI");
  const [apiKey, setApiKey] = React.useState("");
  const [modelName, setModelName] = React.useState("gpt-4o");
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

  if (!open) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    try {
      await createProviderMutation.mutateAsync({
        providerType,
        displayName: `${providerType} Provider`,
        modelName,
        apiKey: apiKey || "local-no-key",
      });
      setApiKey("");
      onClose();
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Error al registrar proveedor");
    }
  };

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        <div className="p-4 border-b border-zinc-200 dark:border-zinc-800 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Cpu className="w-5 h-5 text-blue-500" />
            <h2 className="text-base font-bold text-zinc-900 dark:text-zinc-100">Registrar Proveedor de IA</h2>
          </div>
          <button type="button" onClick={onClose} className="text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200">
            <X className="w-4 h-4" />
          </button>
        </div>

        <form onSubmit={(e) => void handleSubmit(e)} className="p-6 space-y-5">
          {errorMessage && (
            <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-lg flex items-center gap-2 text-xs text-red-500">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{errorMessage}</span>
            </div>
          )}

          <div>
            <label className="text-xs font-semibold text-zinc-700 dark:text-zinc-300 block mb-2">
              Tipo de Proveedor / Runtime
            </label>
            <div className="grid grid-cols-4 gap-2">
              {(["OpenAI", "Gemini", "Ollama", "Local"] as const).map((type) => (
                <button
                  key={type}
                  type="button"
                  onClick={() => {
                    setProviderType(type);
                    if (type === "Gemini") setModelName("gemini-1.5-pro");
                    if (type === "Ollama") setModelName("llama3:70b");
                    if (type === "Local") setModelName("mistral-7b-instruct");
                  }}
                  className={`p-2.5 rounded-xl border text-xs font-semibold flex flex-col items-center gap-1 transition-all ${
                    providerType === type
                      ? "border-blue-500 bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400"
                      : "border-zinc-200 dark:border-zinc-800 bg-zinc-50 dark:bg-zinc-950/40 text-zinc-600 dark:text-zinc-400"
                  }`}
                >
                  <Cpu className="w-4 h-4" />
                  <span>{type}</span>
                </button>
              ))}
            </div>
          </div>

          {(providerType === "OpenAI" || providerType === "Gemini") && (
            <div className="space-y-2">
              <label className="text-xs font-semibold text-zinc-700 dark:text-zinc-300 block">
                API Key del Proveedor
              </label>
              <div className="relative">
                <input
                  type="password"
                  required
                  placeholder="sk-..."
                  value={apiKey}
                  onChange={(e) => setApiKey(e.target.value)}
                  className="w-full bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-xs font-mono"
                />
                <Lock className="w-3.5 h-3.5 absolute right-3 top-2.5 text-amber-500" />
              </div>
            </div>
          )}

          <div className="space-y-2">
            <label className="text-xs font-semibold text-zinc-700 dark:text-zinc-300 block">
              Modelo por Defecto
            </label>
            <input
              type="text"
              required
              value={modelName}
              onChange={(e) => setModelName(e.target.value)}
              className="w-full bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-xs font-mono"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t border-zinc-200 dark:border-zinc-800">
            <button type="button" onClick={onClose} className="px-4 py-2 rounded-lg text-xs text-zinc-500">
              Cancelar
            </button>
            <button
              type="submit"
              disabled={createProviderMutation.isPending}
              className="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold flex items-center gap-1.5 disabled:opacity-50"
            >
              {createProviderMutation.isPending && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
              Guardar en Vault
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
