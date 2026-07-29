import React from "react";
import { MessageSquare, CheckCircle2, RefreshCw, Power } from "lucide-react";
import { ChannelConnection } from "../api/useChannelsData";

interface ChannelCardProps {
  channel: ChannelConnection;
  onTest: (id: string) => void;
  isTesting: boolean;
}

export function ChannelCard({ channel, onTest, isTesting }: ChannelCardProps) {
  const getProviderColor = (provider: string) => {
    switch (provider) {
      case "Telegram":
        return "bg-sky-500/10 text-sky-500 border-sky-500/20";
      case "WhatsApp":
        return "bg-emerald-500/10 text-emerald-500 border-emerald-500/20";
      case "Google":
        return "bg-amber-500/10 text-amber-500 border-amber-500/20";
      default:
        return "bg-purple-500/10 text-purple-500 border-purple-500/20";
    }
  };

  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4 hover:border-zinc-300 dark:hover:border-zinc-700 transition-all">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className={`w-10 h-10 rounded-xl flex items-center justify-center font-bold text-sm border ${getProviderColor(channel.provider)}`}>
            <MessageSquare className="w-5 h-5" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{channel.name}</h3>
            <p className="text-xs text-zinc-400 font-mono">{channel.accountIdentifier}</p>
          </div>
        </div>

        {channel.status === "connected" ? (
          <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 px-2.5 py-0.5 rounded-full border border-emerald-500/20">
            <CheckCircle2 className="w-3.5 h-3.5" /> Conectado
          </span>
        ) : (
          <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-zinc-400 bg-zinc-100 dark:bg-zinc-800 px-2.5 py-0.5 rounded-full">
            <Power className="w-3.5 h-3.5" /> Desconectado
          </span>
        )}
      </div>

      <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <div>
          <span className="text-zinc-400 text-[10px]">Mensajes / 24h</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">{channel.messagesHandled24h.toLocaleString()}</p>
        </div>
        <div>
          <span className="text-zinc-400 text-[10px]">Latencia Webhook</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">{channel.latencyMs > 0 ? `${channel.latencyMs} ms` : "N/A"}</p>
        </div>
      </div>

      <div className="flex items-center justify-between pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <span className="text-[11px] text-zinc-400 font-mono">Sincronización: {channel.lastSync}</span>
        <button
          onClick={() => onTest(channel.id)}
          disabled={isTesting || channel.status !== "connected"}
          className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-zinc-50 dark:bg-zinc-800/60 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-700 transition-colors disabled:opacity-40"
        >
          <RefreshCw className={`w-3 h-3 ${isTesting ? "animate-spin" : ""}`} />
          <span>Probar Diagnóstico</span>
        </button>
      </div>
    </div>
  );
}
