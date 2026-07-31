"use client";

import React from "react";
import { X, Bot, AlertCircle, Loader2 } from "lucide-react";
import { useCreateAgentMutation } from "../api/useAgentsData";

interface CreateAgentModalProps {
 open: boolean;
 onClose: () => void;
}

export function CreateAgentModal({ open, onClose }: CreateAgentModalProps) {
 const [name, setName] = React.useState("");
 const [description, setDescription] = React.useState("");
 const [systemPrompt, setSystemPrompt] = React.useState(
    "Eres un agente de OCAP. Responde con claridad y usa solo las herramientas permitidas."
  );
 const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

 const createAgentMutation = useCreateAgentMutation();

 if (!open) return null;

 const handleSubmit = async (e: React.FormEvent) => {
 e.preventDefault();
 setErrorMessage(null);

 try {
 await createAgentMutation.mutateAsync({
 name: name.trim(),
 description: description.trim(),
 systemPrompt: systemPrompt.trim(),
 allowedTools: []
 });

 setName("");
 setDescription("");
 onClose();
 } catch (err: unknown) {
 const msg = err instanceof Error ? err.message : "Error al conectar con la API de Agentes.";
 setErrorMessage(msg);
 }
 };

 return (
 <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
 <div className="bg-white border border-neutral-200 rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden animate-in fade-in zoom-in-95 duration-150">
 <div className="p-4 border-b border-neutral-200 flex items-center justify-between">
 <div className="flex items-center gap-2">
 <Bot className="w-5 h-5 text-neutral-700" />
 <h2 className="text-base font-bold text-neutral-950">Crear agente</h2>
 </div>
 <button onClick={onClose} className="text-neutral-500 hover:text-neutral-600">
 <X className="w-4 h-4" />
 </button>
 </div>

 <form onSubmit={handleSubmit} className="p-6 space-y-4">
 {errorMessage && (
 <div className="p-3 bg-white border-2 border-neutral-950 rounded-md flex items-center gap-2 text-xs text-neutral-950">
 <AlertCircle className="w-4 h-4 shrink-0" />
 <span>{errorMessage}</span>
 </div>
 )}

 <>
 <div className="space-y-1">
 <label className="text-xs font-semibold text-neutral-700 block">
 Nombre del Agente
 </label>
 <input
 type="text"
 required
 placeholder="ej. FinanceAuditAgent"
 value={name}
 onChange={(e) => setName(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs text-neutral-950 focus:outline-none focus:ring-1 focus:ring-neutral-950"
 />
 </div>

 <div className="space-y-1">
 <label className="text-xs font-semibold text-neutral-700 block">
 Descripción del Propósito
 </label>
 <textarea
 rows={2}
 required
 placeholder="Describe la responsabilidad especializada del agente..."
 value={description}
 onChange={(e) => setDescription(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs text-neutral-950 focus:outline-none focus:ring-1 focus:ring-neutral-950"
 />
 </div>

 <div className="space-y-1">
 <label className="text-xs font-semibold text-neutral-700 block">
 Instrucción de Sistema (System Prompt)
 </label>
 <textarea
 rows={2}
 value={systemPrompt}
 onChange={(e) => setSystemPrompt(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs text-neutral-950 focus:outline-none focus:ring-1 focus:ring-neutral-950"
 />
 </div>

 <div className="flex justify-end gap-2 pt-2 border-t border-neutral-200">
 <button
 type="button"
 onClick={onClose}
 disabled={createAgentMutation.isPending}
 className="px-4 py-2 rounded-lg text-xs font-medium text-neutral-600 hover:bg-neutral-100 disabled:opacity-50"
 >
 Cancelar
 </button>
 <button
 type="submit"
 disabled={createAgentMutation.isPending}
 className="px-4 py-2 rounded-lg bg-neutral-950 hover:bg-neutral-900 text-white text-xs font-semibold shadow-md transition-colors flex items-center gap-1.5 disabled:opacity-50"
 >
 {createAgentMutation.isPending && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
 <span>{createAgentMutation.isPending ? "Creando en API..." : "Crear Agente"}</span>
 </button>
 </div>
 </>
 </form>
 </div>
 </div>
 );
}
