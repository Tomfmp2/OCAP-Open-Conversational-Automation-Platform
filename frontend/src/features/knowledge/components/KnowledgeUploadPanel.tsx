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
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center gap-2">
        <Upload className="w-4 h-4 text-blue-500" />
        <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Subir Documento</h2>
      </div>

      {!selectedKb ? (
        <p className="text-xs text-zinc-500">Seleccione una base de conocimiento para subir documentos.</p>
      ) : (
        <>
          {error && (
            <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-lg flex items-center gap-2 text-xs text-red-500">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <p className="text-xs text-zinc-500">
            Destino: <span className="font-semibold text-zinc-700 dark:text-zinc-300">{selectedKb.name}</span>
          </p>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-700 dark:text-zinc-300 block">Categoría</label>
            <select
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              className="w-full bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-xs"
            >
              <option value="General">General</option>
              <option value="Policy">Policy</option>
              <option value="Technical">Technical</option>
              <option value="FAQ">FAQ</option>
            </select>
          </div>

          <label className="flex flex-col items-center justify-center gap-2 p-6 border-2 border-dashed border-zinc-300 dark:border-zinc-700 rounded-xl cursor-pointer hover:border-blue-500 transition-colors">
            <Upload className="w-6 h-6 text-zinc-400" />
            <span className="text-xs text-zinc-500">
              {isUploading ? "Subiendo..." : "PDF, DOCX, MD, CSV, JSON, HTML, TXT"}
            </span>
            {isUploading && <Loader2 className="w-4 h-4 animate-spin text-blue-500" />}
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
