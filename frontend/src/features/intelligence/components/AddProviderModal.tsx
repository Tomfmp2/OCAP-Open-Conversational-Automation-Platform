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
 apiKey: apiKey.trim(),
 });
 setApiKey("");
 onClose();
 } catch (err: unknown) {
 setErrorMessage(err instanceof Error ? err.message : "Error al registrar proveedor");
 }
 };

 return (
 <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
 <div className="bg-white border border-neutral-200 rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden animate-in fade-in zoom-in-95 duration-150">
 <div className="p-4 border-b border-neutral-200 flex items-center justify-between">
 <div className="flex items-center gap-2">
 <Cpu className="w-5 h-5 text-neutral-700" />
 <h2 className="text-base font-bold text-neutral-950">Registrar Proveedor de IA</h2>
 </div>
 <button type="button" onClick={onClose} className="text-neutral-500 hover:text-neutral-600">
 <X className="w-4 h-4" />
 </button>
 </div>

 <form onSubmit={(e) => void handleSubmit(e)} className="p-6 space-y-5">
 {errorMessage && (
 <div className="p-3 bg-white border-2 border-neutral-950 rounded-md flex items-center gap-2 text-xs text-neutral-950">
 <AlertCircle className="w-4 h-4 shrink-0" />
 <span>{errorMessage}</span>
 </div>
 )}

 <div>
 <label className="text-xs font-semibold text-neutral-700 block mb-2">
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
 ? "border-neutral-950 bg-neutral-100 text-neutral-800"
 : "border-neutral-200 bg-neutral-50 text-neutral-600"
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
 <label className="text-xs font-semibold text-neutral-700 block">
 API Key del Proveedor
 </label>
 <div className="relative">
 <input
 type="password"
 required
 placeholder="sk-..."
 value={apiKey}
 onChange={(e) => setApiKey(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs font-mono"
 />
 <Lock className="w-3.5 h-3.5 absolute right-3 top-2.5 text-neutral-600" />
 </div>
 </div>
 )}

 <div className="space-y-2">
 <label className="text-xs font-semibold text-neutral-700 block">
 Modelo por Defecto
 </label>
 <input
 type="text"
 required
 value={modelName}
 onChange={(e) => setModelName(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs font-mono"
 />
 </div>

 <div className="flex justify-end gap-2 pt-2 border-t border-neutral-200">
 <button type="button" onClick={onClose} className="px-4 py-2 rounded-lg text-xs text-neutral-500">
 Cancelar
 </button>
 <button
 type="submit"
 disabled={createProviderMutation.isPending}
 className="px-4 py-2 rounded-lg bg-neutral-950 hover:bg-neutral-900 text-white text-xs font-semibold flex items-center gap-1.5 disabled:opacity-50"
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
