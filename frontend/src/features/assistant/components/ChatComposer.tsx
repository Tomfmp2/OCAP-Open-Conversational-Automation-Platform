"use client";

import React from "react";
import { ArrowUp, Loader2, Paperclip, RotateCcw, X } from "lucide-react";
import { cn } from "@/shared/utils/cn";

interface ChatComposerProps {
  value: string;
  onChange: (value: string) => void;
  onSubmit: () => void;
  onReset?: () => void;
  pending: boolean;
  providerUsed: string | null;
  expanded: boolean;
  inputRef: React.Ref<HTMLTextAreaElement>;
  files: File[];
  onFilesChange: (files: File[]) => void;
}

/** Mismos límites que la API: el servidor vuelve a comprobarlos por el contenido de cada archivo. */
export const MAX_ATTACHMENTS = 4;
const ACCEPT = "image/png,image/jpeg,image/webp,image/gif,video/mp4,video/webm,video/quicktime,.pdf,.docx,.xlsx,.txt,.md,.csv,.json";
const isVideo = (file: File) => file.type.startsWith("video/");

/** Caja de mensaje del chat: Enter envía, Shift+Enter agrega una línea. */
export function ChatComposer({
  value,
  onChange,
  onSubmit,
  onReset,
  pending,
  providerUsed,
  expanded,
  inputRef,
  files,
  onFilesChange,
}: ChatComposerProps) {
  const fileInputRef = React.useRef<HTMLInputElement>(null);
  const addFiles = (picked: FileList | null) => {
    if (!picked) return;
    onFilesChange([...files, ...Array.from(picked)].slice(0, MAX_ATTACHMENTS));
  };

  const onKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      onSubmit();
    }
  };

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onSubmit();
      }}
      className={cn(
        "ocap-composer w-full rounded-2xl border border-neutral-200 bg-white text-left shadow-sm",
        "transition-[border-color,box-shadow] focus-within:border-neutral-400 focus-within:shadow-md"
      )}
    >
      <label htmlFor="assistant-input" className="sr-only">
        Mensaje para el asistente
      </label>
      <textarea
        id="assistant-input"
        ref={inputRef}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={onKeyDown}
        rows={expanded ? 3 : 2}
        placeholder="Escribe tu pregunta"
        disabled={pending}
        className="block max-h-48 w-full resize-none bg-transparent px-4 pt-3.5 text-base leading-relaxed text-neutral-950 placeholder:text-neutral-500 focus:outline-none disabled:opacity-60"
      />

      {files.length > 0 && (
        <div className="space-y-2 px-3 pt-2">
          <ul className="flex flex-wrap gap-2" aria-label="Archivos adjuntos">
            {files.map((file, i) => (
              <li key={`${file.name}-${i}`} className="inline-flex max-w-full items-center gap-1 rounded-full bg-neutral-100 py-1 pl-3 pr-1 text-xs text-neutral-800">
                <span className="truncate">{file.name}</span>
                <button
                  type="button"
                  onClick={() => onFilesChange(files.filter((_, j) => j !== i))}
                  aria-label={`Quitar ${file.name}`}
                  disabled={pending}
                  className="focus-ring flex h-6 w-6 items-center justify-center rounded-full hover:bg-neutral-200"
                >
                  <X className="h-3 w-3" aria-hidden />
                </button>
              </li>
            ))}
          </ul>
          {files.some(isVideo) && (
            <p className="text-xs text-warning">El video gasta mucha más cuota que el texto y solo lo leen los modelos de Gemini (hasta 15 MB).</p>
          )}
        </div>
      )}

      <div className="flex items-center justify-between gap-3 px-3 pb-3 pt-2">
        <div className="flex min-w-0 items-center gap-2">
          <input
            ref={fileInputRef}
            type="file"
            multiple
            accept={ACCEPT}
            className="sr-only"
            tabIndex={-1}
            aria-hidden
            onChange={(e) => {
              addFiles(e.target.files);
              e.target.value = "";
            }}
          />
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            disabled={pending || files.length >= MAX_ATTACHMENTS}
            aria-label="Adjuntar archivos"
            title="Imágenes, video o documentos (hasta 4)"
            className="focus-ring flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-neutral-100 text-neutral-600 transition-colors hover:bg-neutral-200 hover:text-neutral-950 disabled:opacity-40"
          >
            <Paperclip className="h-4 w-4" aria-hidden />
          </button>
          {onReset && (
            <button
              type="button"
              onClick={onReset}
              aria-label="Nueva conversación"
              title="Nueva conversación"
              className="focus-ring flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-neutral-100 text-neutral-600 transition-colors hover:bg-neutral-200 hover:text-neutral-950"
            >
              <RotateCcw className="h-4 w-4" aria-hidden />
            </button>
          )}
          {providerUsed && <span className="truncate pl-1 text-xs text-neutral-600">Respondió {providerUsed}</span>}
        </div>

        <button
          type="submit"
          disabled={pending || (!value.trim() && files.length === 0)}
          aria-label="Enviar"
          className="focus-ring flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-neutral-950 text-white transition-colors hover:bg-neutral-800 disabled:bg-neutral-200 disabled:text-neutral-500"
        >
          {pending ? <Loader2 className="h-4 w-4 animate-spin motion-reduce:animate-none" aria-hidden /> : <ArrowUp className="h-4 w-4" aria-hidden />}
        </button>
      </div>
    </form>
  );
}
