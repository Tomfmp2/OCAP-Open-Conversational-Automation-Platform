import React from "react";
import { Terminal, Info, AlertTriangle, AlertCircle, Filter } from "lucide-react";
import { AuditLogEntry } from "../api/useMonitoringData";
import { EmptyState, Surface } from "@/shared/components/ui";

interface AuditLogViewerProps {
  logs: AuditLogEntry[];
}

export function AuditLogViewer({ logs }: AuditLogViewerProps) {
  const [filterLevel, setFilterLevel] = React.useState<"all" | "info" | "warning" | "error">("all");

  const filteredLogs = logs.filter((log) => {
    if (filterLevel === "all") return true;
    if (filterLevel === "warning") return log.level === "warning" || log.level === "warn";
    return log.level === filterLevel;
  });

  return (
    <Surface variant="glass" className="space-y-4">
      <div className="flex flex-col justify-between gap-3 border-b border-zinc-800/80 pb-3 sm:flex-row sm:items-center">
        <div className="flex items-center gap-2">
          <Terminal className="h-4 w-4 text-violet-400" />
          <h2 className="text-sm font-semibold text-zinc-100">Actividad de auditoría</h2>
        </div>

        <div className="flex items-center gap-1.5 text-xs">
          <Filter className="mr-1 h-3.5 w-3.5 text-zinc-500" />
          {(["all", "info", "warning", "error"] as const).map((lvl) => (
            <button
              type="button"
              key={lvl}
              onClick={() => setFilterLevel(lvl)}
              className={`rounded-lg px-2.5 py-1 font-mono text-[10px] uppercase transition-colors ${
                filterLevel === lvl
                  ? "bg-blue-600 font-bold text-white"
                  : "bg-zinc-900 text-zinc-400 hover:bg-zinc-800"
              }`}
            >
              {lvl === "all" ? "Todos" : lvl === "warning" ? "Avisos" : lvl}
            </button>
          ))}
        </div>
      </div>

      {filteredLogs.length === 0 ? (
        <EmptyState
          title={logs.length === 0 ? "Sin eventos de auditoría" : "Sin resultados"}
          description={
            logs.length === 0
              ? "La API no devolvió actividad reciente."
              : "No hay eventos que coincidan con el filtro seleccionado."
          }
        />
      ) : (
        <div className="max-h-80 space-y-2 overflow-y-auto font-mono text-xs">
          {filteredLogs.map((log) => (
          <div
            key={log.id}
            className={`flex items-start justify-between gap-3 rounded-xl border p-3 ${
              log.level === "error"
                ? "bg-red-500/10 border-red-500/20 text-red-400"
                : log.level === "warning" || log.level === "warn"
                ? "bg-amber-500/10 border-amber-500/20 text-amber-400"
                : "bg-zinc-950 border-zinc-800 text-zinc-300"
            }`}
          >
            <div className="flex items-start gap-2.5 min-w-0">
              {log.level === "error" ? (
                <AlertCircle className="w-4 h-4 text-red-500 shrink-0 mt-0.5" />
              ) : log.level === "warning" || log.level === "warn" ? (
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
      )}
    </Surface>
  );
}
