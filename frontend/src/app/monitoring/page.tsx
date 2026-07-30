"use client";

import React from "react";
import { Activity, Cpu, HardDrive, RefreshCw } from "lucide-react";
import { useMonitoringData } from "@/features/monitoring/api/useMonitoringData";
import { SystemMetricsChart } from "@/features/monitoring/components/SystemMetricsChart";
import { AuditLogViewer } from "@/features/monitoring/components/AuditLogViewer";
import { MonitoringSkeleton } from "@/features/monitoring/components/MonitoringSkeleton";
import { Button, ErrorState, MetricCard, PageHeader } from "@/shared/components/ui";

export default function MonitoringPage() {
  const { data, isLoading, isError, error, refetch, isFetching } = useMonitoringData();

  if (isLoading) {
    return <MonitoringSkeleton />;
  }

  if (isError) {
    return (
      <div className="mx-auto max-w-7xl">
        <ErrorState
          title="No se pudo cargar la telemetría"
          message={error instanceof Error ? error.message : undefined}
          onRetry={() => void refetch()}
        />
      </div>
    );
  }

  const { metrics, logs, summary } = data ?? {
    metrics: [],
    logs: [],
    summary: { cpuAverage: 0, memoryPeakMb: 0 },
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Monitorización"
        description="Telemetría del sistema y actividad de auditoría reportadas por la API."
        icon={<Activity className="h-5 w-5 text-blue-400" />}
        actions={
          <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}>
            <RefreshCw className="h-3.5 w-3.5" />
            Actualizar
          </Button>
        }
      />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <MetricCard
          title="CPU promedio observada"
          value={metrics.length > 0 ? `${summary.cpuAverage}%` : "Sin datos"}
          subtitle="Calculada con las muestras recibidas"
          icon={Cpu}
          tone="info"
        />
        <MetricCard
          title="Pico de memoria observado"
          value={metrics.length > 0 ? `${summary.memoryPeakMb} MB` : "Sin datos"}
          subtitle="Máximo entre las muestras recibidas"
          icon={HardDrive}
          tone="accent"
        />
      </div>

      <SystemMetricsChart metrics={metrics} />
      <AuditLogViewer logs={logs} />
    </div>
  );
}
