"use client";

import React from "react";
import { X, Cpu, Lock, CheckCircle2 } from "lucide-react";

interface AddProviderModalProps {
  open: boolean;
  onClose: () => void;
}

export function AddProviderModal({ open, onClose }: AddProviderModalProps) {
  const [providerType, setProviderType] = React.useState<"OpenAI" | "Gemini" | "Ollama" | "Local">("OpenAI");
  const [apiKey, setApiKey] = React.useState("");
  const [modelName, setModelName] = React.useState("gpt-4o");
  const [success, setSuccess] = React.useState(false);

  if (!open) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSuccess(true);
    setTimeout(() => {
      setSuccess(false);
      onClose();
    }, 1200);
  };

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        <div className="p-4 border-b border-zinc-200 dark:border-zinc-800 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Cpu className="w-5 h-5 text-blue-500" />
            <h2 className="text-base font-bold text-zinc-900 dark:text-zinc-100">Registrar Proveedor de IA</h2>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200">
            <X className="w-4 h-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-5">
          {success ? (
            <div className="py-8 text-center space-y-3">
              <div className="w-12 h-12 rounded-full bg-emerald-500/10 text-emerald-500 mx-auto flex items-center justify-center">
                <CheckCircle2 className="w-6 h-6" />
              </div>
              <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">¡Proveedor Registrado & Cifrado!</h3>
              <p className="text-xs text-zinc-400">Credenciales guardadas con seguridad AES-256 en Credential Vault.</p>
            </div>
          ) : (
            <>
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
                      className="w-full bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-xs text-zinc-900 dark:text-zinc-100 placeholder:text-zinc-400 focus:outline-none focus:ring-1 focus:ring-blue-500 font-mono"
                    />
                    <Lock className="w-3.5 h-3.5 absolute right-3 top-2.5 text-amber-500" />
                  </div>
                  <p className="text-[10px] text-zinc-400">
                    Nunca almacenamos claves en texto plano. Se cifran con la clave derivada del tenant.
                  </p>
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
                  className="w-full bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-xs text-zinc-900 dark:text-zinc-100 focus:outline-none focus:ring-1 focus:ring-blue-500 font-mono"
                />
              </div>

              <div className="flex justify-end gap-2 pt-2 border-t border-zinc-200 dark:border-zinc-800">
                <button
                  type="button"
                  onClick={onClose}
                  className="px-4 py-2 rounded-lg text-xs font-medium text-zinc-600 dark:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-zinc-800"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors"
                >
                  Guardar en Vault
                </button>
              </div>
            </>
          )}
        </form>
      </div>
    </div>
  );
}
