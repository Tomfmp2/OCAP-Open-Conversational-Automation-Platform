"use client";

import React from "react";
import { Activity, RefreshCw, Cpu, HardDrive, ShieldCheck, Zap } from "lucide-react";
import { useMonitoringData } from "@/features/monitoring/api/useMonitoringData";
import { SystemMetricsChart } from "@/features/monitoring/components/SystemMetricsChart";
import { AuditLogViewer } from "@/features/monitoring/components/AuditLogViewer";
import { MonitoringSkeleton } from "@/features/monitoring/components/MonitoringSkeleton";

export default function MonitoringPage() {
  const { data, isLoading, refetch, isFetching } = useMonitoringData();

  if (isLoading) {
    return <MonitoringSkeleton />;
  }

  const { metrics, logs, summary } = data || { metrics: [], logs: [], summary: { cpuAverage: 0, memoryPeakMb: 0, uptimePercent: 0, errorRate: "0%" } };

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <Activity className="w-5 h-5 text-blue-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Centro de Monitorización & Observabilidad (Grafana-Style)
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Supervisión técnica de métricas del núcleo OCAP, pools de hilos, base de datos y auditoría de eventos.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors shadow-sm disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            <span>Actualizar Telemetría</span>
          </button>
        </div>
      </div>

      {/* Summary KPI Strip */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 shadow-sm">
          <span className="text-xs text-zinc-500">Uso CPU Promedio</span>
          <p className="text-2xl font-bold text-zinc-900 dark:text-zinc-100 mt-1">{summary.cpuAverage}%</p>
        </div>
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 shadow-sm">
          <span className="text-xs text-zinc-500">Pico RAM Registrado</span>
          <p className="text-2xl font-bold text-purple-500 mt-1">{summary.memoryPeakMb} MB</p>
        </div>
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 shadow-sm">
          <span className="text-xs text-zinc-500">Disponibilidad (Uptime)</span>
          <p className="text-2xl font-bold text-emerald-500 mt-1">{summary.uptimePercent}%</p>
        </div>
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 shadow-sm">
          <span className="text-xs text-zinc-500">Tasa de Error Global</span>
          <p className="text-2xl font-bold text-zinc-900 dark:text-zinc-100 mt-1">{summary.errorRate}</p>
        </div>
      </div>

      {/* Metrics Chart Panels */}
      <SystemMetricsChart metrics={metrics} />

      {/* Audit Logs Stream */}
      <AuditLogViewer logs={logs} />
    </div>
  );
}
