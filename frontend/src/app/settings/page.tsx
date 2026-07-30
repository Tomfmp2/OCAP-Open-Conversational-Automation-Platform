"use client";

import React from "react";
import { Settings, RefreshCw } from "lucide-react";
import { useSettingsData, SettingsConfig } from "@/features/settings/api/useSettingsData";
import { SettingsForm } from "@/features/settings/components/SettingsForm";
import { SettingsSkeleton } from "@/features/settings/components/SettingsSkeleton";

export default function SettingsPage() {
  const { data: settings, isLoading, refetch, isFetching, updateSettingsMutation } = useSettingsData();

  if (isLoading || !settings) {
    return <SettingsSkeleton />;
  }

  const handleSave = async (newConfig: SettingsConfig) => {
    await updateSettingsMutation.mutateAsync(newConfig);
  };

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <Settings className="w-5 h-5 text-blue-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Ajustes & Preferencias Globales
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Configuración general del tenant activo, políticas de retención y comportamiento del núcleo.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors shadow-sm disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            <span>Actualizar</span>
          </button>
        </div>
      </div>

      <SettingsForm
        key={`${settings.tenantName}-${settings.timezone}-${settings.auditLogRetentionDays}`}
        settings={settings}
        onSave={handleSave}
        isSaving={updateSettingsMutation.isPending}
        saveSuccess={updateSettingsMutation.isSuccess}
      />
    </div>
  );
}
