"use client";

import React from "react";
import {
  Activity,
  Cpu,
  MessageSquare,
  ShieldCheck,
  TrendingUp,
  RefreshCw,
  Zap,
  Radio,
  Clock,
} from "lucide-react";
import { useDashboardData } from "@/features/dashboard/api/useDashboardData";
import { useSignalR } from "@/shared/utils/useSignalR";
import { DashboardSkeleton } from "@/features/dashboard/components/DashboardSkeleton";
import { DashboardErrorState } from "@/features/dashboard/components/DashboardErrorState";

export default function OverviewPage() {
  const { data, isLoading, isError, error, refetch, isFetching } = useDashboardData();
  const { connectionState, liveEvents, reconnect } = useSignalR();

  if (isLoading) {
    return <DashboardSkeleton />;
  }

  if (isError || !data) {
    return <DashboardErrorState onRetry={() => refetch()} errorMessage={error?.message} />;
  }

  const { metrics, overview } = data;

  const METRIC_CARDS = [
    {
      title: "Ejecuciones Totales",
      value: (metrics?.totalExecutions || overview?.workflows?.executionsToday || 0).toLocaleString(),
      change: "+12%",
      period: "Ejecuciones de workflows hoy",
      icon: Activity,
    },
    {
      title: "Canales Activos",
      value: `${overview?.channels?.connectedCount || metrics?.activeChannelsCount || 0} / ${overview?.channels?.totalCount || metrics?.totalChannelsCount || 0}`,
      change: "100%",
      period: "Telegram & WhatsApp listos",
      icon: MessageSquare,
    },
    {
      title: "Costo Estimado IA",
      value: `$${(metrics?.monthlyAiCostUsd || 14.5).toFixed(2)}`,
      change: "-3%",
      period: "Consumo mensual estimado",
      icon: Cpu,
    },
    {
      title: "Salud del Sistema",
      value: overview?.health === "Healthy" ? "100%" : "85%",
      change: overview?.health || "Excelente",
      period: `Uptime: ${overview?.uptime?.uptimeFormatted || "0m"}`,
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

        <div className="flex items-center gap-3">
          {/* SignalR Connection Status Badge */}
          <div
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg border text-xs font-medium ${
              connectionState === "Connected"
                ? "bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800"
                : connectionState === "Reconnecting" || connectionState === "Connecting"
                ? "bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 border-amber-200 dark:border-amber-800 animate-pulse"
                : "bg-red-50 dark:bg-red-950/40 text-red-700 dark:text-red-300 border-red-200 dark:border-red-800"
            }`}
          >
            <Radio className="w-3.5 h-3.5" />
            <span>
              {connectionState === "Connected"
                ? "SignalR Live Gateway"
                : connectionState === "Reconnecting"
                ? "Reconectando Gateway..."
                : connectionState === "Connecting"
                ? "Conectando..."
                : "Gateway Desconectado"}
            </span>
            {connectionState === "Disconnected" && (
              <button onClick={reconnect} className="underline text-[10px] ml-1 font-bold">
                Reconectar
              </button>
            )}
          </div>

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

      {/* Real-time Activity and SignalR Live Feed Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Backend Audit Logs */}
        <div className="lg:col-span-2 space-y-4">
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm">
            <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3 mb-4">
              <div className="flex items-center gap-2">
                <Clock className="w-4 h-4 text-blue-500" />
                <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">Actividad Reciente del Sistema</h3>
              </div>
              <span className="text-[11px] text-zinc-400">REST API / AuditLog</span>
            </div>

            {overview?.lastActivity && overview.lastActivity.length > 0 ? (
              <div className="divide-y divide-zinc-100 dark:divide-zinc-800/60 space-y-2">
                {overview.lastActivity.map((log) => (
                  <div key={log.id} className="pt-2 flex items-start justify-between gap-4 text-xs">
                    <div>
                      <span className="font-semibold text-zinc-800 dark:text-zinc-200">{log.eventType}</span>
                      <p className="text-zinc-500 text-[11px] mt-0.5">{log.description}</p>
                    </div>
                    <div className="text-right text-[11px] text-zinc-400 whitespace-nowrap">
                      <span>{new Date(log.occurredAtUtc).toLocaleTimeString()}</span>
                      <p className="text-[10px] text-zinc-500">{log.source}</p>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-xs text-zinc-500 text-center py-6">
                No hay actividad registrada en la base de datos aún.
              </p>
            )}
          </div>
        </div>

        {/* SignalR Live Gateway Streaming Logs */}
        <div className="space-y-4">
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm">
            <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3 mb-4">
              <div className="flex items-center gap-2">
                <Zap className="w-4 h-4 text-amber-500" />
                <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">Eventos en Vivo (SignalR)</h3>
              </div>
              <span className="text-[10px] font-medium px-2 py-0.5 rounded bg-amber-500/10 text-amber-600 dark:text-amber-400">
                {liveEvents.length} eventos
              </span>
            </div>

            {liveEvents.length > 0 ? (
              <div className="space-y-2.5 max-h-[300px] overflow-y-auto pr-1">
                {liveEvents.map((evt) => (
                  <div key={evt.id} className="p-2 rounded bg-zinc-50 dark:bg-zinc-800/50 text-xs border border-zinc-100 dark:border-zinc-800">
                    <div className="flex items-center justify-between">
                      <span className="font-semibold text-blue-600 dark:text-blue-400">{evt.eventName}</span>
                      <span className="text-[10px] text-zinc-400">{new Date(evt.timestamp).toLocaleTimeString()}</span>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8 space-y-1">
                <Radio className="w-5 h-5 text-zinc-400 mx-auto animate-pulse" />
                <p className="text-xs text-zinc-500">Escuchando canal SignalR...</p>
                <p className="text-[10px] text-zinc-400">Los eventos en tiempo real aparecerán aquí instantáneamente.</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
