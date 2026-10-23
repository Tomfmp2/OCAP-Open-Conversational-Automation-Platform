"use client";

import React from "react";
import { ArrowUp, Loader2, RefreshCw } from "lucide-react";

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
      className="ocap-composer w-full overflow-hidden rounded-2xl border border-neutral-200 bg-white shadow-[0_1px_0_rgba(0,0,0,0.04)] transition-shadow focus-within:border-neutral-400"
    >
      <div className="flex items-center justify-between border-b border-neutral-100 px-4 py-2">
        <p className="text-[11px] text-neutral-500">
          {description}
          {providerUsed ? (
            <span className="ml-2 font-mono text-neutral-400">· {providerUsed}</span>
          ) : null}
        </p>
        {onReset && (
          <button
            type="button"
            onClick={onReset}
            className="inline-flex items-center gap-1 text-[11px] font-medium text-neutral-500 transition-colors hover:text-neutral-950"
          >
            <RefreshCw className="h-3 w-3" />
            Nueva
          </button>
        )}
      </div>

      <div className="px-4 pt-3">
        <textarea
          ref={inputRef}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onKeyDown={onKeyDown}
          rows={expanded ? 3 : 2}
          placeholder="Pregúntame cualquier cosa…"
          disabled={pending}
          className="w-full resize-none bg-transparent text-[15px] leading-relaxed text-neutral-950 placeholder:text-neutral-400 focus:outline-none disabled:opacity-60"
        />
      </div>

      <div className="flex items-center justify-between gap-3 px-3 pb-3 pt-1">
        <p className="pl-1 text-[10px] text-neutral-400">Enter para enviar · Shift+Enter nueva línea</p>
        <button
          type="submit"
          disabled={pending || !value.trim()}
          aria-label="Enviar"
          className="flex h-9 w-9 items-center justify-center rounded-full bg-neutral-950 text-white transition-opacity disabled:opacity-30 hover:bg-neutral-800"
        >
          {pending ? <Loader2 className="h-4 w-4 animate-spin" /> : <ArrowUp className="h-4 w-4" />}
        </button>
      </div>
    </form>
  );
}
