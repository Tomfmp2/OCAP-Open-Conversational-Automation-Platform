"use client";

import React from "react";
import {
  Activity,
  Cpu,
  MessageSquare,
  ShieldCheck,
  TrendingUp,
  RefreshCw,
  Plus,
  Inbox,
} from "lucide-react";
import { useDashboardData } from "@/features/dashboard/api/useDashboardData";
import { DashboardSkeleton } from "@/features/dashboard/components/DashboardSkeleton";
import { DashboardErrorState } from "@/features/dashboard/components/DashboardErrorState";

export default function OverviewPage() {
  const { data, isLoading, isError, error, refetch, isFetching } = useDashboardData();

  if (isLoading) {
    return <DashboardSkeleton />;
  }

  if (isError || !data) {
    return <DashboardErrorState onRetry={() => refetch()} errorMessage={error?.message} />;
  }

  const { metrics, conversations, costTrends, throughputTrends, agentStatus } = data;

  const METRIC_CARDS = [
    {
      title: "Ejecuciones Totales",
      value: (metrics?.totalExecutions || 0).toLocaleString(),
      change: metrics?.executionsChange || "0%",
      period: "vs mes anterior",
      icon: Activity,
    },
    {
      title: "Canales Activos",
      value: `${metrics?.activeChannelsCount || 0} / ${metrics?.totalChannelsCount || 0}`,
      change: "100%",
      period: "Adaptadores Omnichannel",
      icon: MessageSquare,
    },
    {
      title: "Costo Estimado IA",
      value: `$${(metrics?.monthlyAiCostUsd || 0).toFixed(2)}`,
      change: metrics?.aiCostChange || "0%",
      period: "Consumo mensual USD",
      icon: Cpu,
    },
    {
      title: "Salud del Sistema",
      value: `${metrics?.systemHealthPercentage || 100}%`,
      change: "Excelente",
      period: "Uptime Núcleo OCAP",
      icon: ShieldCheck,
    },
  ];

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
            Dashboard Enterprise SaaS
          </h1>
          <p className="text-xs text-zinc-500 mt-1">
            Supervisión integral de agentes autónomos, consumo de modelos IA, canales y salud operacional.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors shadow-sm disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            <span>{isFetching ? "Sincronizando..." : "Sincronizar"}</span>
          </button>
        </div>
      </div>

      {/* KPI Metric Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {METRIC_CARDS.map((kpi, idx) => {
          const Icon = kpi.icon;
          return (
            <div
              key={idx}
              className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 shadow-sm hover:border-zinc-300 dark:hover:border-zinc-700 transition-all"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-medium text-zinc-500">{kpi.title}</span>
                <div className="p-2 rounded-lg bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400">
                  <Icon className="w-4 h-4" />
                </div>
              </div>
              <div className="mt-3 flex items-baseline justify-between">
                <span className="text-3xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">{kpi.value}</span>
                <span className="inline-flex items-center gap-0.5 text-xs font-semibold px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
                  <TrendingUp className="w-3 h-3" />
                  {kpi.change}
                </span>
              </div>
              <p className="mt-1 text-[11px] text-zinc-400">{kpi.period}</p>
            </div>
          );
        })}
      </div>

      {/* Main Grid: Empty States for Activity & Conversations */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-12 text-center space-y-3">
            <Inbox className="w-8 h-8 text-zinc-400 mx-auto" />
            <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-100">No hay métricas ni actividad disponible</h3>
            <p className="text-xs text-zinc-500">
              Conecta adaptadores de comunicación o registra proveedores de IA para visualizar telemetría en tiempo real.
            </p>
          </div>
        </div>

        <div>
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-8 text-center space-y-2">
            <Inbox className="w-6 h-6 text-zinc-400 mx-auto" />
            <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">No existen conversaciones activas</h3>
            <p className="text-xs text-zinc-500">Las conversaciones entrantes de Telegram o WhatsApp aparecerán aquí.</p>
          </div>
        </div>
      </div>
    </div>
  );
}
