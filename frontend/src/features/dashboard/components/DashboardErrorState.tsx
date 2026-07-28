import React from "react";
import { AlertTriangle, RefreshCw } from "lucide-react";

interface DashboardErrorStateProps {
  onRetry: () => void;
  errorMessage?: string;
}

export function DashboardErrorState({ onRetry, errorMessage }: DashboardErrorStateProps) {
  return (
    <div className="max-w-xl mx-auto my-12 p-8 bg-white dark:bg-zinc-900 border border-red-200 dark:border-red-900/50 rounded-2xl shadow-xl text-center space-y-4">
      <div className="w-12 h-12 rounded-full bg-red-100 dark:bg-red-950/60 text-red-600 dark:text-red-400 mx-auto flex items-center justify-center">
        <AlertTriangle className="w-6 h-6" />
      </div>
      <div>
        <h2 className="text-lg font-bold text-zinc-900 dark:text-zinc-100">Error al cargar métricas del Dashboard</h2>
        <p className="text-xs text-zinc-500 mt-1">
          {errorMessage || "No se pudo sincronizar el estado en tiempo real con el API Gateway de OCAP."}
        </p>
      </div>
      <button
        onClick={onRetry}
        className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors"
      >
        <RefreshCw className="w-4 h-4" />
        <span>Reintentar Conexión</span>
      </button>
    </div>
  );
}
