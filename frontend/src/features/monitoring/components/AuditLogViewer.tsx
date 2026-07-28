import React from "react";
import { Terminal, Info, AlertTriangle, AlertCircle, Filter } from "lucide-react";
import { AuditLogEntry } from "../api/useMonitoringData";

interface AuditLogViewerProps {
  logs: AuditLogEntry[];
}

export function AuditLogViewer({ logs }: AuditLogViewerProps) {
  const [filterLevel, setFilterLevel] = React.useState<"all" | "info" | "warning" | "error">("all");

  const filteredLogs = logs.filter((l) => (filterLevel === "all" ? true : l.level === filterLevel));

  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <Terminal className="w-4 h-4 text-emerald-500" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
            Centro de Auditoría & Stream de Logs Grafana-Style
          </h2>
        </div>

        {/* Level Filters */}
        <div className="flex items-center gap-1.5 text-xs">
          <Filter className="w-3.5 h-3.5 text-zinc-400 mr-1" />
          {(["all", "info", "warning", "error"] as const).map((lvl) => (
            <button
              key={lvl}
              onClick={() => setFilterLevel(lvl)}
              className={`px-2.5 py-0.5 rounded-md font-mono text-[11px] uppercase transition-colors ${
                filterLevel === lvl
                  ? "bg-blue-600 text-white font-bold"
                  : "bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 hover:bg-zinc-200"
              }`}
            >
              {lvl}
            </button>
          ))}
        </div>
      </div>

      <div className="space-y-2 font-mono text-xs max-h-80 overflow-y-auto">
        {filteredLogs.map((log) => (
          <div
            key={log.id}
            className={`p-3 rounded-lg border flex items-start justify-between gap-3 ${
              log.level === "error"
                ? "bg-red-500/10 border-red-500/20 text-red-400"
                : log.level === "warning"
                ? "bg-amber-500/10 border-amber-500/20 text-amber-400"
                : "bg-zinc-950 border-zinc-800 text-zinc-300"
            }`}
          >
            <div className="flex items-start gap-2.5 min-w-0">
              {log.level === "error" ? (
                <AlertCircle className="w-4 h-4 text-red-500 shrink-0 mt-0.5" />
              ) : log.level === "warning" ? (
                <AlertTriangle className="w-4 h-4 text-amber-500 shrink-0 mt-0.5" />
              ) : (
                <Info className="w-4 h-4 text-blue-400 shrink-0 mt-0.5" />
              )}
              <div className="min-w-0">
                <span className="text-[10px] font-bold uppercase tracking-wider text-zinc-400">[{log.source}]</span>
                <p className="text-xs leading-relaxed mt-0.5">{log.message}</p>
              </div>
            </div>
            <span className="text-[10px] text-zinc-500 shrink-0">{log.timestamp}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
