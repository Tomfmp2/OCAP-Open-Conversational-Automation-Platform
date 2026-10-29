"use client";

import React from "react";
import { ArrowUp, Loader2, RotateCcw } from "lucide-react";
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
}

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
}: ChatComposerProps) {
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

      <div className="flex items-center justify-between gap-3 px-3 pb-3 pt-2">
        <div className="flex min-w-0 items-center gap-2">
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
          disabled={pending || !value.trim()}
          aria-label="Enviar"
          className="focus-ring flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-neutral-950 text-white transition-colors hover:bg-neutral-800 disabled:bg-neutral-200 disabled:text-neutral-500"
        >
          {pending ? <Loader2 className="h-4 w-4 animate-spin motion-reduce:animate-none" aria-hidden /> : <ArrowUp className="h-4 w-4" aria-hidden />}
        </button>
      </div>
    </form>
  );
}
