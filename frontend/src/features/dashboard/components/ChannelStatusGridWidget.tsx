import React from "react";
import { Radio, CheckCircle2, MessageSquare, ExternalLink } from "lucide-react";

export function ChannelStatusGridWidget() {
  const CHANNELS = [
    { name: "Telegram Bot Native", status: "Online", adapter: "CAP-01 Telegram Adapter", messages24h: 4210 },
    { name: "WhatsApp Business Cloud", status: "Online", adapter: "CAP-01 WhatsApp Adapter", messages24h: 3890 },
    { name: "Google Workspace Gmail", status: "Online", adapter: "CAP-01 Gmail Adapter", messages24h: 1450 },
    { name: "Slack Enterprise Grid", status: "Online", adapter: "CAP-01 Slack Adapter", messages24h: 980 },
  ];

  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <Radio className="w-4 h-4 text-blue-500 animate-pulse" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Red de Adaptadores de Canales</h2>
        </div>
        <button className="text-xs text-blue-500 hover:underline flex items-center gap-1">
          <span>Gestionar</span>
          <ExternalLink className="w-3 h-3" />
        </button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {CHANNELS.map((ch, idx) => (
          <div
            key={idx}
            className="p-3.5 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 flex items-center justify-between hover:border-zinc-300 dark:hover:border-zinc-700 transition-colors"
          >
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-lg bg-blue-500/10 text-blue-500 flex items-center justify-center font-bold text-xs shrink-0">
                <MessageSquare className="w-4 h-4" />
              </div>
              <div>
                <p className="text-xs font-semibold text-zinc-900 dark:text-zinc-100">{ch.name}</p>
                <p className="text-[10px] text-zinc-400 font-mono">{ch.messages24h.toLocaleString()} msgs / 24h</p>
              </div>
            </div>

            <span className="inline-flex items-center gap-1 text-[10px] font-semibold text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 px-2 py-0.5 rounded-full border border-emerald-500/20 shrink-0">
              <CheckCircle2 className="w-3 h-3" /> {ch.status}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
