"use client";

import React from "react";
import { useRouter } from "next/navigation";
import { ArrowRight, Search, X } from "lucide-react";
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
    <Modal open={open} onClose={handleClose} title="Buscar en OCAP" className="max-w-xl p-0">
      <div className="-m-5">
        <div className="flex items-center gap-3 border-b border-neutral-200 p-3">
          <Search className="h-4 w-4 shrink-0 text-neutral-400" />
          <input
            type="text"
            autoFocus
            placeholder="Buscar módulos…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="flex-1 bg-transparent text-sm text-neutral-950 placeholder:text-neutral-400 focus:outline-none"
          />
          <button
            type="button"
            onClick={handleClose}
            className="text-neutral-400 hover:text-neutral-700"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="max-h-80 space-y-1 overflow-y-auto p-2">
          {filtered.length === 0 ? (
            <p className="p-4 text-center text-xs text-neutral-500">
              No hay resultados para &quot;{query}&quot;
            </p>
          ) : (
            filtered.map((item) => (
              <button
                key={item.href + item.label}
                type="button"
                onClick={() => handleSelect(item.href)}
                className="group flex w-full items-center justify-between rounded-md px-3 py-2.5 text-left transition-colors hover:bg-neutral-100"
              >
                <div>
                  <p className="text-xs font-semibold text-neutral-950">{item.label}</p>
                  <p className="text-[10px] text-neutral-500">{item.category}</p>
                </div>
                <ArrowRight className="h-3.5 w-3.5 text-neutral-400 opacity-0 transition-opacity group-hover:opacity-100" />
              </button>
            ))
          )}
        </div>
      </div>
    </Modal>
  );
}
