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
  description: string;
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
  description,
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
        "ocap-composer w-full rounded-3xl border border-neutral-200/80 bg-white text-left",
        "shadow-[0_18px_50px_-22px_rgba(255,90,40,0.28),0_2px_6px_-2px_rgba(10,10,10,0.06)]",
        "transition-shadow focus-within:border-neutral-300 focus-within:shadow-[0_22px_60px_-22px_rgba(255,90,40,0.38),0_2px_6px_-2px_rgba(10,10,10,0.08)]"
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
        rows={expanded ? 2 : 1}
        placeholder="Pregúntame cualquier cosa…"
        disabled={pending}
        className="block w-full resize-none bg-transparent px-5 pt-4 text-[15px] leading-relaxed text-neutral-950 placeholder:text-neutral-400 focus:outline-none disabled:opacity-60"
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
          <span className="truncate pl-2 text-[11px] text-neutral-400">
            {description}
            {providerUsed && <span className="font-mono"> · {providerUsed}</span>}
          </span>
        </div>

        <button
          type="submit"
          disabled={pending || !value.trim()}
          aria-label="Enviar"
          className="focus-ring flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-neutral-950 text-white shadow-[0_6px_16px_-6px_rgba(10,10,10,0.5)] transition-[transform,opacity] hover:scale-105 active:scale-95 disabled:scale-100 disabled:opacity-25"
        >
          {pending ? <Loader2 className="h-4 w-4 animate-spin motion-reduce:animate-none" aria-hidden /> : <ArrowUp className="h-4 w-4" aria-hidden />}
        </button>
      </div>
    </form>
  );
}
