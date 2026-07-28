import React from "react";
import { Radio, Plus, CheckCircle2 } from "lucide-react";
import { WebhookItem } from "../api/useDeveloperData";

interface WebhookManagerProps {
  webhooks: WebhookItem[];
}

export function WebhookManager({ webhooks }: WebhookManagerProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <Radio className="w-4 h-4 text-purple-500" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Endpoints de Webhooks</h2>
        </div>
        <button className="flex items-center gap-1.5 px-3 py-1 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-zinc-50 dark:bg-zinc-800 text-xs font-semibold hover:bg-zinc-100 transition-colors">
          <Plus className="w-3.5 h-3.5" />
          <span>Registrar Endpoint</span>
        </button>
      </div>

      <div className="space-y-3">
        {webhooks.map((w) => (
          <div
            key={w.id}
            className="p-3.5 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 space-y-2 text-xs"
          >
            <div className="flex items-center justify-between">
              <span className="font-mono font-semibold text-zinc-900 dark:text-zinc-100">{w.url}</span>
              <span className="inline-flex items-center gap-1 text-[10px] font-semibold text-emerald-500 bg-emerald-500/10 px-2 py-0.5 rounded-full border border-emerald-500/20">
                <CheckCircle2 className="w-3 h-3" /> Activo
              </span>
            </div>
            <div className="flex flex-wrap gap-1">
              {w.events.map((ev, idx) => (
                <span key={idx} className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-zinc-200 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400">
                  {ev}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
