import React from "react";
import { MessageSquare, ArrowUpRight, CheckCircle2, Clock } from "lucide-react";
import { ConversationSummary } from "../api/useDashboardData";

interface RecentConversationsWidgetProps {
  conversations: ConversationSummary[];
}

export function RecentConversationsWidget({ conversations }: RecentConversationsWidgetProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Conversaciones Recientes</h2>
        <button className="text-xs text-blue-500 hover:underline flex items-center gap-0.5">
          <span>Ver todas</span>
          <ArrowUpRight className="w-3 h-3" />
        </button>
      </div>

      {conversations.length === 0 ? (
        <div className="py-8 text-center text-xs text-zinc-400">
          No hay conversaciones activas en este momento.
        </div>
      ) : (
        <div className="space-y-3">
          {conversations.map((conv) => (
            <div
              key={conv.id}
              className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/40 border border-zinc-200 dark:border-zinc-800/60 flex items-start justify-between gap-3 hover:border-zinc-300 dark:hover:border-zinc-700 transition-colors"
            >
              <div className="flex items-start gap-3 min-w-0">
                <div className="w-8 h-8 rounded-lg bg-blue-500/10 text-blue-500 flex items-center justify-center font-bold text-xs shrink-0 mt-0.5">
                  <MessageSquare className="w-4 h-4" />
                </div>
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="text-xs font-semibold text-zinc-900 dark:text-zinc-100 truncate">{conv.senderName}</p>
                    <span className="text-[10px] px-1.5 py-0.2 rounded bg-zinc-200 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 font-mono">
                      {conv.channel}
                    </span>
                  </div>
                  <p className="text-xs text-zinc-500 truncate mt-0.5">{conv.lastMessage}</p>
                </div>
              </div>

              <div className="flex flex-col items-end shrink-0">
                <span className="text-[10px] text-zinc-400 font-mono">{conv.timestamp}</span>
                {conv.status === "active" ? (
                  <span className="mt-1 flex items-center gap-1 text-[10px] font-medium text-emerald-500">
                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" /> Activa
                  </span>
                ) : conv.status === "resolved" ? (
                  <span className="mt-1 flex items-center gap-1 text-[10px] text-zinc-400">
                    <CheckCircle2 className="w-3 h-3 text-emerald-500" /> Resuelta
                  </span>
                ) : (
                  <span className="mt-1 flex items-center gap-1 text-[10px] text-amber-500">
                    <Clock className="w-3 h-3" /> Pendiente
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
