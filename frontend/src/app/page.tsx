"use client";

import React from "react";
import Link from "next/link";
import {
  Activity,
  Bot,
  BookOpen,
  GitFork,
  MessageSquare,
  Radio,
  RefreshCw,
  ShieldCheck,
  Zap,
} from "lucide-react";
import { useDashboardData } from "@/features/dashboard/api/useDashboardData";
import { useSignalR } from "@/shared/utils/useSignalR";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { DashboardSkeleton } from "@/features/dashboard/components/DashboardSkeleton";
import {
  Badge,
  Button,
  EmptyState,
  ErrorState,
  MetricCard,
  PageHeader,
  Surface,
} from "@/shared/components/ui";

const MODULE_LINKS = [
  { href: "/agents", label: "Agentes", icon: Bot, hint: "Catálogo y creación" },
  { href: "/channels", label: "Canales", icon: MessageSquare, hint: "Telegram & WhatsApp" },
  { href: "/workflows", label: "Workflows", icon: GitFork, hint: "Automatización" },
  { href: "/knowledge", label: "Knowledge", icon: BookOpen, hint: "RAG y búsqueda" },
];

export default function OverviewPage() {
  const { user } = useAuth();
  const { data, isLoading, isError, error, refetch, isFetching } = useDashboardData();
  const { connectionState, liveEvents, reconnect } = useSignalR(user?.tenantId);

  if (isLoading) return <DashboardSkeleton />;

  if (isError || !data) {
    return (
      <ErrorState
        message={error?.message}
        onRetry={() => void refetch()}
      />
    );
  }

  const { metrics, overview } = data;
  const health = overview?.health || "Unknown";

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title={`Welcome back${user?.fullName ? `, ${user.fullName.split(" ")[0]}` : ""}`}
        description="Supervisión de agentes, canales, workflows y salud operacional — solo datos reales de la API."
        actions={
          <>
            <Badge
              tone={
                connectionState === "Connected"
                  ? "success"
                  : connectionState === "Disconnected"
                    ? "danger"
                    : "warning"
              }
            >
              <Radio className="h-3 w-3" />
              SignalR {connectionState}
            </Badge>
            {connectionState === "Disconnected" && (
              <Button size="sm" variant="secondary" onClick={() => void reconnect()}>
                Reconectar
              </Button>
            )}
            <Button
              size="sm"
              variant="secondary"
              onClick={() => void refetch()}
              loading={isFetching}
            >
              <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? "animate-spin" : ""}`} />
              Sincronizar
            </Button>
          </>
        }
      />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          title="Ejecuciones hoy"
          value={(metrics?.totalExecutions || overview?.workflows?.executionsToday || 0).toLocaleString()}
          subtitle={`${overview?.workflows?.activeCount ?? 0} workflows activos`}
          icon={Activity}
          tone="info"
        />
        <MetricCard
          title="Canales conectados"
          value={`${overview?.channels?.connectedCount ?? metrics?.activeChannelsCount ?? 0}/${overview?.channels?.totalCount ?? metrics?.totalChannelsCount ?? 0}`}
          subtitle="Conexiones registradas"
          icon={MessageSquare}
          tone="accent"
        />
        <MetricCard
          title="Agentes"
          value={overview?.agents?.totalCount ?? 0}
          subtitle={`${overview?.agents?.activeCount ?? 0} activos · ${overview?.agents?.runtimeStatus || "N/D"}`}
          icon={Bot}
          tone="neutral"
        />
        <MetricCard
          title="Salud del sistema"
          value={health}
          subtitle={`Uptime ${overview?.uptime?.uptimeFormatted || "N/D"}`}
          icon={ShieldCheck}
          tone={health.toLowerCase() === "healthy" ? "success" : "warning"}
        />
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {MODULE_LINKS.map((mod) => {
          const Icon = mod.icon;
          return (
            <Link key={mod.href} href={mod.href}>
              <Surface
                variant="glass"
                className="h-full transition-all hover:border-blue-500/40 hover:shadow-[0_0_30px_rgba(59,130,246,0.12)]"
              >
                <div className="flex items-center gap-3">
                  <div className="rounded-xl bg-violet-500/10 p-2 text-violet-400">
                    <Icon className="h-4 w-4" />
                  </div>
                  <div>
                    <p className="text-sm font-bold text-zinc-900 dark:text-zinc-100">
                      {mod.label}
                    </p>
                    <p className="text-[11px] text-zinc-500">{mod.hint}</p>
                  </div>
                </div>
              </Surface>
            </Link>
          );
        })}
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3" id="activity">
        <Surface className="lg:col-span-2" padding="md">
          <div className="mb-4 flex items-center justify-between border-b border-zinc-100 pb-3 dark:border-zinc-800">
            <div className="flex items-center gap-2">
              <Activity className="h-4 w-4 text-blue-500" />
              <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">
                Actividad reciente
              </h3>
            </div>
            <span className="text-[11px] text-zinc-400">AuditLog</span>
          </div>

          {overview?.lastActivity?.length ? (
            <div className="divide-y divide-zinc-100 dark:divide-zinc-800/60">
              {overview.lastActivity.map((log) => (
                <div
                  key={log.id}
                  className="flex items-start justify-between gap-4 py-3 text-xs"
                >
                  <div>
                    <p className="font-semibold text-zinc-800 dark:text-zinc-200">
                      {log.eventType}
                    </p>
                    <p className="mt-0.5 text-[11px] text-zinc-500">{log.description}</p>
                  </div>
                  <div className="shrink-0 text-right text-[11px] text-zinc-400">
                    <p>{new Date(log.occurredAtUtc).toLocaleString()}</p>
                    <p className="text-[10px] text-zinc-500">{log.source}</p>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState
              title="Sin actividad registrada"
              description="Los eventos de auditoría aparecerán aquí cuando el sistema genere actividad."
            />
          )}
        </Surface>

        <Surface id="live" padding="md">
          <div className="mb-4 flex items-center justify-between border-b border-zinc-100 pb-3 dark:border-zinc-800">
            <div className="flex items-center gap-2">
              <Zap className="h-4 w-4 text-amber-500" />
              <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">
                Eventos en vivo
              </h3>
            </div>
            <Badge tone="warning">{liveEvents.length}</Badge>
          </div>

          {liveEvents.length > 0 ? (
            <div className="max-h-[320px] space-y-2 overflow-y-auto pr-1">
              {liveEvents.map((evt) => (
                <div
                  key={evt.id}
                  className="rounded-xl border border-zinc-100 bg-zinc-50 p-2 text-xs dark:border-zinc-800 dark:bg-zinc-800/40"
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-semibold text-blue-600 dark:text-blue-400">
                      {evt.eventName}
                    </span>
                    <span className="text-[10px] text-zinc-400">
                      {new Date(evt.timestamp).toLocaleTimeString()}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="space-y-2 py-8 text-center">
              <Radio className="mx-auto h-5 w-5 animate-pulse text-zinc-400" />
              <p className="text-xs text-zinc-500">
                {connectionState === "Connected"
                  ? "Escuchando canal SignalR…"
                  : "Tiempo real no disponible hasta reconectar."}
              </p>
            </div>
          )}
        </Surface>
      </div>

      <Surface variant="glass" className="grid gap-4 sm:grid-cols-3" padding="md">
        <div>
          <p className="text-[11px] text-zinc-500">Usuarios</p>
          <p className="mt-1 text-xl font-bold">
            {overview?.users?.activeCount ?? 0}
            <span className="text-sm font-normal text-zinc-500">
              /{overview?.users?.totalCount ?? 0}
            </span>
          </p>
        </div>
        <div>
          <p className="text-[11px] text-zinc-500">API Keys activas</p>
          <p className="mt-1 text-xl font-bold">
            {overview?.apiKeys?.activeCount ?? 0}
          </p>
        </div>
        <div>
          <p className="text-[11px] text-zinc-500">Webhooks</p>
          <p className="mt-1 text-xl font-bold">
            {overview?.webhooks?.activeSubscriptions ?? 0}
            <span className="text-sm font-normal text-zinc-500">
              {" "}
              · {overview?.webhooks?.deliveriesToday ?? 0} deliveries hoy
            </span>
          </p>
        </div>
      </Surface>
    </div>
  );
}
