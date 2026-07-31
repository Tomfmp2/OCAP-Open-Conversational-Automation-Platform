"use client";

import React from "react";
import { X, BookOpen, Loader2, AlertCircle } from "lucide-react";
import type { CreateKnowledgeBasePayload } from "../api/useKnowledgeData";

interface CreateKnowledgeBaseModalProps {
 open: boolean;
 onClose: () => void;
 onCreate: (payload: CreateKnowledgeBasePayload) => Promise<void>;
 isCreating: boolean;
 error?: string | null;
}

export function CreateKnowledgeBaseModal({
 open,
 onClose,
 onCreate,
 isCreating,
 error,
}: CreateKnowledgeBaseModalProps) {
 const [name, setName] = React.useState("");
 const [description, setDescription] = React.useState("");
 const [strategy, setStrategy] = React.useState("Semantic");
 const [vectorDbProvider, setVectorDbProvider] = React.useState("PgVector");

 if (!open) return null;

 const handleSubmit = async (e: React.FormEvent) => {
 e.preventDefault();
 await onCreate({ name: name.trim(), description: description.trim(), strategy, vectorDbProvider });
 setName("");
 setDescription("");
 onClose();
 };

 return (
 <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
 <div className="bg-white border border-neutral-200 rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden">
 <div className="p-4 border-b border-neutral-200 flex items-center justify-between">
 <div className="flex items-center gap-2">
 <BookOpen className="w-5 h-5 text-neutral-700" />
 <h2 className="text-base font-bold text-neutral-950">Nueva Base de Conocimiento</h2>
 </div>
 <button type="button" onClick={onClose} className="text-neutral-500 hover:text-neutral-800">
 <X className="w-4 h-4" />
 </button>
 </div>

 <form onSubmit={(e) => void handleSubmit(e)} className="p-6 space-y-4">
 {error && (
 <div className="p-3 bg-white border-2 border-neutral-950 rounded-md flex items-center gap-2 text-xs text-neutral-950">
 <AlertCircle className="w-4 h-4 shrink-0" />
 <span>{error}</span>
 </div>
 )}

 <div className="space-y-1">
 <label className="text-xs font-semibold text-neutral-700 block">Nombre</label>
 <input
 type="text"
 required
 value={name}
 onChange={(e) => setName(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs"
 />
 </div>

 <div className="space-y-1">
 <label className="text-xs font-semibold text-neutral-700 block">Descripción</label>
 <textarea
 rows={2}
 value={description}
 onChange={(e) => setDescription(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs"
 />
 </div>

 <div className="grid grid-cols-2 gap-3">
 <div className="space-y-1">
 <label className="text-xs font-semibold text-neutral-700 block">Estrategia</label>
 <select
 value={strategy}
 onChange={(e) => setStrategy(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs"
 >
 <option value="Semantic">Semantic</option>
 <option value="FixedSize">FixedSize</option>
 <option value="Recursive">Recursive</option>
 </select>
 </div>
 <div className="space-y-1">
 <label className="text-xs font-semibold text-neutral-700 block">Vector DB</label>
 <select
 value={vectorDbProvider}
 onChange={(e) => setVectorDbProvider(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs"
 >
 <option value="PgVector">PgVector</option>
 <option value="Qdrant">Qdrant</option>
 <option value="InMemory">InMemory</option>
 </select>
 </div>
 </div>

 <div className="flex justify-end gap-2 pt-2 border-t border-neutral-200">
 <button type="button" onClick={onClose} className="px-4 py-2 rounded-lg text-xs text-neutral-500">
 Cancelar
 </button>
 <button
 type="submit"
 disabled={isCreating}
 className="px-4 py-2 rounded-lg bg-neutral-950 hover:bg-neutral-900 text-white text-xs font-semibold flex items-center gap-1.5 disabled:opacity-50"
 >
 {isCreating && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
 Crear KB
 </button>
 </div>
 </form>
 </div>
 </div>
 );
}
