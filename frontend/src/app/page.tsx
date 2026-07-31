"use client";

import React from "react";
import Link from "next/link";
import {
  Activity,
  Bot,
  BookOpen,
  GitFork,
  MessageSquare,
  RefreshCw,
  Cpu,
} from "lucide-react";
import { useDashboardData } from "@/features/dashboard/api/useDashboardData";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { DashboardSkeleton } from "@/features/dashboard/components/DashboardSkeleton";
import {
  Button,
  EmptyState,
  ErrorState,
  MetricCard,
  PageHeader,
  Surface,
} from "@/shared/components/ui";

const MODULE_LINKS = [
  { href: "/agents", label: "Agentes", icon: Bot, hint: "Crear y gestionar agentes" },
  { href: "/channels", label: "Canales", icon: MessageSquare, hint: "Telegram y WhatsApp" },
  { href: "/workflows", label: "Workflows", icon: GitFork, hint: "Automatizaciones" },
  { href: "/knowledge", label: "Conocimiento", icon: BookOpen, hint: "Bases y búsqueda" },
  { href: "/intelligence", label: "IA y modelos", icon: Cpu, hint: "Proveedores de modelos" },
];

export default function OverviewPage() {
  const { user } = useAuth();
  const { data, isLoading, isError, error, refetch, isFetching } = useDashboardData();

  if (isLoading) return <DashboardSkeleton />;

  if (isError || !data) {
    return (
      <ErrorState
        message={error?.message}
        onRetry={() => void refetch()}
      />
    );
  }

  const { overview } = data;
  const health = overview?.health || "Desconocido";
  const agentsTotal = overview?.agents?.totalCount ?? 0;
  const channelsConnected = overview?.channels?.connectedCount ?? 0;
  const workflowsActive = overview?.workflows?.activeCount ?? 0;

  return (
    <div className="mx-auto max-w-5xl space-y-8">
      <PageHeader
        title="Resumen"
        description={
          user?.email
            ? `Sesión: ${user.email}`
            : "Estado real de agentes, canales y workflows en este entorno."
        }
        actions={
          <Button
            size="sm"
            variant="secondary"
            onClick={() => void refetch()}
            loading={isFetching}
          >
            <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? "animate-spin" : ""}`} />
            Actualizar
          </Button>
        }
      />

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          title="Agentes"
          value={agentsTotal}
          subtitle={
            agentsTotal === 0
              ? "Ninguno registrado"
              : `${overview?.agents?.activeCount ?? 0} activos`
          }
          icon={Bot}
        />
        <MetricCard
          title="Canales conectados"
          value={`${channelsConnected}/${overview?.channels?.totalCount ?? 0}`}
          subtitle={
            channelsConnected === 0
              ? "Conecta Telegram o WhatsApp"
              : "Conexiones registradas"
          }
          icon={MessageSquare}
        />
        <MetricCard
          title="Workflows activos"
          value={workflowsActive}
          subtitle={`${overview?.workflows?.executionsToday ?? 0} ejecuciones hoy`}
          icon={GitFork}
        />
        <MetricCard
          title="Salud API"
          value={health}
          subtitle={
            overview?.uptime?.uptimeFormatted
              ? `Uptime ${overview.uptime.uptimeFormatted}`
              : "Sin dato de uptime"
          }
          icon={Activity}
        />
      </div>

      {(agentsTotal === 0 || channelsConnected === 0) && (
        <Surface className="space-y-2 border-neutral-400" padding="md">
          <p className="text-sm font-semibold text-neutral-950">Primeros pasos</p>
          <ul className="space-y-1 text-sm text-neutral-600">
            {agentsTotal === 0 && (
              <li>
                Aún no hay agentes.{" "}
                <Link href="/agents" className="font-medium underline underline-offset-2">
                  Crear uno
                </Link>
              </li>
            )}
            {channelsConnected === 0 && (
              <li>
                Ningún canal conectado.{" "}
                <Link href="/channels" className="font-medium underline underline-offset-2">
                  Conectar canal
                </Link>
              </li>
            )}
            <li>
              Configura un proveedor de IA en{" "}
              <Link href="/intelligence" className="font-medium underline underline-offset-2">
                IA y modelos
              </Link>
            </li>
          </ul>
        </Surface>
      )}

      <div>
        <h2 className="mb-3 text-sm font-semibold text-neutral-950">Módulos</h2>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {MODULE_LINKS.map((mod) => {
            const Icon = mod.icon;
            return (
              <Link key={mod.href} href={mod.href}>
                <Surface className="h-full transition-colors hover:border-neutral-950" padding="md">
                  <div className="flex items-center gap-3">
                    <div className="rounded-md border border-neutral-200 bg-neutral-50 p-2 text-neutral-800">
                      <Icon className="h-4 w-4" />
                    </div>
                    <div>
                      <p className="text-sm font-semibold text-neutral-950">{mod.label}</p>
                      <p className="text-[11px] text-neutral-500">{mod.hint}</p>
                    </div>
                  </div>
                </Surface>
              </Link>
            );
          })}
        </div>
      </div>

      <Surface padding="md">
        <div className="mb-4 flex items-center justify-between border-b border-neutral-200 pb-3">
          <h3 className="text-sm font-semibold text-neutral-950">Actividad reciente</h3>
          <span className="text-[11px] text-neutral-400">Auditoría</span>
        </div>

        {overview?.lastActivity?.length ? (
          <div className="divide-y divide-neutral-100">
            {overview.lastActivity.map((log) => (
              <div
                key={log.id}
                className="flex items-start justify-between gap-4 py-3 text-xs"
              >
                <div>
                  <p className="font-semibold text-neutral-900">{log.eventType}</p>
                  <p className="mt-0.5 text-[11px] text-neutral-500">{log.description}</p>
                </div>
                <div className="shrink-0 text-right font-mono text-[11px] text-neutral-400">
                  <p>{new Date(log.occurredAtUtc).toLocaleString()}</p>
                  <p>{log.source}</p>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <EmptyState
            title="Sin actividad registrada"
            description="Los eventos aparecerán cuando uses agentes, canales o workflows."
          />
        )}
      </Surface>
    </div>
  );
}
