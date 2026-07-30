"use client";

import React from "react";
import { ShieldCheck, RefreshCw, Inbox } from "lucide-react";
import { useInstallerData } from "@/features/installer/api/useInstallerData";
import { InstallerWizardSteps } from "@/features/installer/components/InstallerWizardSteps";
import { InstallerSkeleton } from "@/features/installer/components/InstallerSkeleton";

export default function InstallerPage() {
  const { data, isLoading, isError, error, refetch, isFetching } =
    useInstallerData();

  if (isLoading) {
    return <InstallerSkeleton />;
  }

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <ShieldCheck className="w-5 h-5 text-blue-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Installer Center & Diagnostics (Asistente de Instalación)
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Diagnóstico público de PostgreSQL, Event Bus, almacenamiento y health checks agregados.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors shadow-sm disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            <span>Revisar Estado</span>
          </button>
        </div>
      </div>

      {isError ? (
        <div
          role="alert"
          className="bg-white dark:bg-zinc-900 border border-red-200 dark:border-red-900 rounded-xl p-12 text-center space-y-3"
        >
          <Inbox className="w-6 h-6 text-red-500 mx-auto" />
          <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">
            No se pudo ejecutar el diagnóstico
          </h3>
          <p className="text-xs text-zinc-500">
            {error instanceof Error
              ? error.message
              : "El servicio de diagnóstico no está disponible."}
          </p>
          <button
            type="button"
            onClick={() => void refetch()}
            disabled={isFetching}
            className="rounded-lg bg-blue-600 px-4 py-2 text-xs font-semibold text-white hover:bg-blue-500 disabled:opacity-50"
          >
            Reintentar
          </button>
        </div>
      ) : !data || data.steps.length === 0 ? (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-12 text-center space-y-3">
          <Inbox className="w-6 h-6 text-zinc-400 mx-auto" />
          <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">No hay información de instalación disponible</h3>
          <p className="text-xs text-zinc-500">El diagnóstico respondió sin componentes registrados.</p>
        </div>
      ) : (
        <InstallerWizardSteps
          steps={data.steps}
          isSystemReady={data.isSystemReady}
          isValidating={isFetching}
          lastCheckedAt={data.timestamp}
          onValidate={() => void refetch()}
        />
      )}
    </div>
  );
}
