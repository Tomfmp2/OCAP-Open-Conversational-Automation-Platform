"use client";

import React from "react";
import { Stethoscope, RefreshCw } from "lucide-react";
import { useInstallerData } from "@/features/installer/api/useInstallerData";
import { InstallerWizardSteps } from "@/features/installer/components/InstallerWizardSteps";
import { InstallerSkeleton } from "@/features/installer/components/InstallerSkeleton";
import { Button, EmptyState, ErrorState, PageHeader } from "@/shared/components/ui";

export default function InstallerPage() {
  const { data, isLoading, isError, error, refetch, isFetching } =
    useInstallerData();

  if (isLoading) {
    return <InstallerSkeleton />;
  }

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Diagnóstico de instalación"
        description="Comprobación pública de las dependencias reportadas por el servicio de salud."
        icon={<Stethoscope className="h-5 w-5 text-violet-400" />}
        actions={
          <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}>
            <RefreshCw className="h-3.5 w-3.5" />
            Revisar estado
          </Button>
        }
      />

      {isError ? (
        <ErrorState
          title="No se pudo ejecutar el diagnóstico"
          message={error instanceof Error ? error.message : "El servicio de diagnóstico no está disponible."}
          onRetry={() => void refetch()}
        />
      ) : !data || data.steps.length === 0 ? (
        <EmptyState
          title="Sin información de instalación"
          description="El diagnóstico respondió sin componentes registrados."
        />
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
