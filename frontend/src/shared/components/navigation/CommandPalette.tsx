"use client";

import React from "react";
import { useRouter } from "next/navigation";
import { ArrowRight, Search, Sparkles, X } from "lucide-react";
import { COMMAND_ITEMS } from "@/shared/config/navigation";
import { Modal } from "@/shared/components/ui";

interface CommandPaletteProps {
  open: boolean;
  onClose: () => void;
}

export function CommandPalette({ open, onClose }: CommandPaletteProps) {
  const router = useRouter();
  const [query, setQuery] = React.useState("");

  const filtered = COMMAND_ITEMS.filter(
    (cmd) =>
      cmd.label.toLowerCase().includes(query.toLowerCase()) ||
      cmd.category.toLowerCase().includes(query.toLowerCase())
  );

  const handleSelect = (href: string) => {
    router.push(href);
    setQuery("");
    onClose();
  };

  const handleClose = () => {
    setQuery("");
    onClose();
  };

  return (
    <Modal open={open} onClose={handleClose} title="Command Palette" className="max-w-xl p-0">
      <div className="-m-5">
        <div className="flex items-center gap-3 border-b border-zinc-200 p-3 dark:border-zinc-800">
          <Search className="h-4 w-4 shrink-0 text-zinc-400" />
          <input
            type="text"
            autoFocus
            placeholder="Buscar módulos y acciones…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="flex-1 bg-transparent text-sm text-zinc-900 placeholder:text-zinc-400 focus:outline-none dark:text-zinc-100"
          />
          <button
            type="button"
            onClick={handleClose}
            className="text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="max-h-80 space-y-1 overflow-y-auto p-2">
          {filtered.length === 0 ? (
            <p className="p-4 text-center text-xs text-zinc-500">
              No hay resultados para &quot;{query}&quot;
            </p>
          ) : (
            filtered.map((item) => (
              <button
                key={item.href + item.label}
                type="button"
                onClick={() => handleSelect(item.href)}
                className="group flex w-full items-center justify-between rounded-lg px-3 py-2.5 text-left transition-colors hover:bg-blue-50 dark:hover:bg-blue-950/40"
              >
                <div className="flex items-center gap-3">
                  <Sparkles className="h-4 w-4 text-zinc-400 group-hover:text-blue-500" />
                  <div>
                    <p className="text-xs font-semibold text-zinc-900 group-hover:text-blue-600 dark:text-zinc-100 dark:group-hover:text-blue-400">
                      {item.label}
                    </p>
                    <p className="text-[10px] text-zinc-400">{item.category}</p>
                  </div>
                </div>
                <ArrowRight className="h-3.5 w-3.5 text-blue-500 opacity-0 transition-opacity group-hover:opacity-100" />
              </button>
            ))
          )}
        </div>
      </div>
    </Modal>
  );
}
