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
      <div className="flex flex-col justify-between gap-3 border-b border-neutral-200 pb-3 sm:flex-row sm:items-center">
        <div className="flex items-center gap-2">
          <Terminal className="h-4 w-4 text-neutral-700" />
          <h2 className="text-sm font-semibold text-neutral-950">Actividad de auditoría</h2>
        </div>

        <div className="flex items-center gap-1.5 text-xs">
          <Filter className="mr-1 h-3.5 w-3.5 text-neutral-500" />
          {(["all", "info", "warning", "error"] as const).map((lvl) => (
            <button
              type="button"
              key={lvl}
              onClick={() => setFilterLevel(lvl)}
              aria-pressed={filterLevel === lvl}
              className={`rounded-md px-2.5 py-1 text-xs transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-neutral-950 ${
                filterLevel === lvl
                  ? "bg-neutral-950 font-medium text-white"
                  : "bg-neutral-100 text-neutral-600 hover:bg-neutral-200"
              }`}
            >
              {lvl === "all" ? "Todos" : lvl === "warning" ? "Avisos" : lvl === "error" ? "Errores" : "Info"}
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
                ? "bg-danger-subtle border-danger/20 text-danger"
                : log.level === "warning" || log.level === "warn"
                ? "bg-warning-subtle border-warning/20 text-warning"
                : "bg-neutral-50 border-neutral-200 text-neutral-700"
            }`}
          >
            <div className="flex items-start gap-2.5 min-w-0">
              {log.level === "error" ? (
                <AlertCircle className="w-4 h-4 text-danger shrink-0 mt-0.5" aria-label="Error" />
              ) : log.level === "warning" || log.level === "warn" ? (
                <AlertTriangle className="w-4 h-4 text-warning shrink-0 mt-0.5" aria-label="Aviso" />
              ) : (
                <Info className="w-4 h-4 text-neutral-500 shrink-0 mt-0.5" aria-label="Información" />
              )}
              <div className="min-w-0">
                <span className="text-xs font-bold uppercase tracking-wider text-neutral-600">[{log.source}]</span>
                <p className="text-xs leading-relaxed mt-0.5">{log.message}</p>
              </div>
            </div>
            <span className="text-xs text-neutral-500 shrink-0">{log.timestamp}</span>
          </div>
          ))}
        </div>
      )}
    </Surface>
  );
}
