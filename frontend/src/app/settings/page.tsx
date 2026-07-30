"use client";

import React from "react";
import { Settings, RefreshCw } from "lucide-react";
import { useSettingsData, SettingsConfig } from "@/features/settings/api/useSettingsData";
import { SettingsForm } from "@/features/settings/components/SettingsForm";
import { SettingsSkeleton } from "@/features/settings/components/SettingsSkeleton";
import { Button, ErrorState, PageHeader } from "@/shared/components/ui";

export default function SettingsPage() {
  const {
    data: settings,
    isLoading,
    isError,
    error,
    refetch,
    isFetching,
    updateSettingsMutation,
  } = useSettingsData();

  if (isLoading) {
    return <SettingsSkeleton />;
  }

  if (isError || !settings) {
    return (
      <div className="mx-auto max-w-7xl">
        <ErrorState
          title="No se pudo cargar la configuración"
          message={error instanceof Error ? error.message : undefined}
          onRetry={() => void refetch()}
        />
      </div>
    );
  }

  const handleSave = async (newConfig: SettingsConfig) => {
    await updateSettingsMutation.mutateAsync(newConfig);
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Ajustes"
        description="Preferencias del tenant, retención y comportamiento operativo."
        icon={<Settings className="h-5 w-5 text-blue-400" />}
        actions={
          <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}>
            <RefreshCw className="h-3.5 w-3.5" />
            Actualizar
          </Button>
        }
      />

      <SettingsForm
        key={`${settings.tenantName}-${settings.timezone}-${settings.auditLogRetentionDays}`}
        settings={settings}
        onSave={handleSave}
        isSaving={updateSettingsMutation.isPending}
        saveSuccess={updateSettingsMutation.isSuccess}
        saveError={
          updateSettingsMutation.error instanceof Error
            ? updateSettingsMutation.error.message
            : updateSettingsMutation.isError
              ? "No se pudieron guardar los cambios."
              : undefined
        }
      />
    </div>
  );
}
