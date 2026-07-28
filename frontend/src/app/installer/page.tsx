"use client";

import React from "react";
import { ShieldCheck, RefreshCw, Inbox } from "lucide-react";
import { useInstallerData } from "@/features/installer/api/useInstallerData";
import { InstallerWizardSteps } from "@/features/installer/components/InstallerWizardSteps";
import { InstallerSkeleton } from "@/features/installer/components/InstallerSkeleton";

export default function InstallerPage() {
  const { data, isLoading, refetch, isFetching } = useInstallerData();

  if (isLoading) {
    return <InstallerSkeleton />;
  }

  const steps = data?.steps || [];

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
            Verifica conexiones PostgreSQL, Credential Vault, IA y canales de comunicación.
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

      {steps.length === 0 ? (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-12 text-center space-y-3">
          <Inbox className="w-6 h-6 text-zinc-400 mx-auto" />
          <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">No hay información de instalación disponible</h3>
          <p className="text-xs text-zinc-500">Ejecuta el asistente de verificación para validar las dependencias del servidor.</p>
        </div>
      ) : (
        <InstallerWizardSteps steps={steps} />
      )}
    </div>
  );
}
