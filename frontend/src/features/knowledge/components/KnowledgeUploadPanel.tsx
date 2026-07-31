"use client";

import React from "react";
import { Upload, Loader2, AlertCircle } from "lucide-react";
import type { KnowledgeBase } from "../api/useKnowledgeData";

interface KnowledgeUploadPanelProps {
 selectedKb: KnowledgeBase | null;
 onUpload: (file: File, category: string) => Promise<void>;
 isUploading: boolean;
 error?: string | null;
}

export function KnowledgeUploadPanel({
 selectedKb,
 onUpload,
 isUploading,
 error,
}: KnowledgeUploadPanelProps) {
 const [category, setCategory] = React.useState("General");
 const fileInputRef = React.useRef<HTMLInputElement>(null);

 const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
 const file = e.target.files?.[0];
 if (!file || !selectedKb) return;
 await onUpload(file, category);
 if (fileInputRef.current) fileInputRef.current.value = "";
 };

 return (
 <div className="bg-white border border-neutral-200 rounded-xl p-5 shadow-sm space-y-4">
 <div className="flex items-center gap-2">
 <Upload className="w-4 h-4 text-neutral-700" />
 <h2 className="text-sm font-semibold text-neutral-950">Subir Documento</h2>
 </div>

 {!selectedKb ? (
 <p className="text-xs text-neutral-500">Seleccione una base de conocimiento para subir documentos.</p>
 ) : (
 <>
 {error && (
 <div className="p-3 bg-white border-2 border-neutral-950 rounded-md flex items-center gap-2 text-xs text-neutral-950">
 <AlertCircle className="w-4 h-4 shrink-0" />
 <span>{error}</span>
 </div>
 )}

 <p className="text-xs text-neutral-500">
 Destino: <span className="font-semibold text-neutral-700">{selectedKb.name}</span>
 </p>

 <div className="space-y-1">
 <label className="text-xs font-semibold text-neutral-700 block">Categoría</label>
 <select
 value={category}
 onChange={(e) => setCategory(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs"
 >
 <option value="General">General</option>
 <option value="Policy">Policy</option>
 <option value="Technical">Technical</option>
 <option value="FAQ">FAQ</option>
 </select>
 </div>

 <label className="flex flex-col items-center justify-center gap-2 p-6 border-2 border-dashed border-neutral-300 rounded-xl cursor-pointer hover:border-neutral-950 transition-colors">
 <Upload className="w-6 h-6 text-neutral-500" />
 <span className="text-xs text-neutral-500">
 {isUploading ? "Subiendo..." : "PDF, DOCX, MD, CSV, JSON, HTML, TXT"}
 </span>
 {isUploading && <Loader2 className="w-4 h-4 animate-spin text-neutral-700" />}
 <input
 ref={fileInputRef}
 type="file"
 className="hidden"
 accept=".pdf,.docx,.md,.markdown,.csv,.json,.html,.htm,.xml,.txt"
 disabled={isUploading}
 onChange={(e) => void handleFileChange(e)}
 />
 </label>
 </>
 )}
 </div>
 );
}
