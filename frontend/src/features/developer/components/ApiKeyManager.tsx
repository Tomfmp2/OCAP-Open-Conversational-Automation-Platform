import React from "react";
import { Key, Plus, Trash2 } from "lucide-react";
import { ApiKeyItem } from "../api/useDeveloperData";

interface ApiKeyManagerProps {
  keys: ApiKeyItem[];
}

export function ApiKeyManager({ keys }: ApiKeyManagerProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <Key className="w-4 h-4 text-blue-500" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">API Keys de Plataforma</h2>
        </div>
        <button className="flex items-center gap-1.5 px-3 py-1 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-sm transition-colors">
          <Plus className="w-3.5 h-3.5" />
          <span>Generar Nueva Clave</span>
        </button>
      </div>

      <div className="space-y-3">
        {keys.map((k) => (
          <div
            key={k.id}
            className="p-3.5 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 flex items-center justify-between text-xs"
          >
            <div>
              <p className="font-semibold text-zinc-900 dark:text-zinc-100">{k.name}</p>
              <p className="font-mono text-zinc-400 mt-0.5">{k.keyPrefix}</p>
            </div>
            <div className="flex items-center gap-3">
              <span className="text-[11px] text-zinc-400">Último uso: {k.lastUsed}</span>
              <button className="text-red-400 hover:text-red-300 p-1">
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
