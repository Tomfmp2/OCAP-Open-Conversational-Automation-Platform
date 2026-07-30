"use client";

import React from "react";
import { ShieldCheck, RefreshCw, Users, MonitorSmartphone } from "lucide-react";
import { useSecurityData } from "@/features/security/api/useSecurityData";
import { RbacMatrixTable } from "@/features/security/components/RbacMatrixTable";
import { VaultSecurityStatus } from "@/features/security/components/VaultSecurityStatus";
import { SecuritySkeleton } from "@/features/security/components/SecuritySkeleton";
import {
  Badge,
  Button,
  EmptyState,
  ErrorState,
  MetricCard,
  PageHeader,
  Surface,
} from "@/shared/components/ui";

export default function SecurityPage() {
  const { data, isLoading, isError, error, refetch, isFetching } = useSecurityData();

  if (isLoading) {
    return <SecuritySkeleton />;
  }

  if (isError || !data) {
    return (
      <div className="mx-auto max-w-7xl">
        <ErrorState
          title="No se pudo cargar seguridad"
          message={error instanceof Error ? error.message : "Revisa la sesión y los permisos e inténtalo de nuevo."}
          onRetry={() => void refetch()}
        />
      </div>
    );
  }

  const { roles, users, sessions, permissions } = data;

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Seguridad"
        description="Roles, permisos y visibilidad disponible del vault para el tenant activo."
        icon={<ShieldCheck className="h-5 w-5 text-violet-400" />}
        actions={
          <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}>
            <RefreshCw className="h-3.5 w-3.5" />
            Actualizar
          </Button>
        }
      />

      <div className="grid grid-cols-1 gap-6">
        <VaultSecurityStatus />
        {roles.length === 0 ? (
          <EmptyState
            title="No hay roles disponibles"
            description="La API no devolvió roles para el tenant activo."
          />
        ) : (
          <RbacMatrixTable roles={roles} />
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <MetricCard
            title="Usuarios"
            value={users.length}
            subtitle={`${users.filter((user) => user.isActive).length} activos`}
            icon={Users}
            tone="info"
          />
          <MetricCard
            title="Sesiones"
            value={sessions.length}
            subtitle={`${sessions.filter((session) => session.isActive).length} activas`}
            icon={MonitorSmartphone}
            tone="accent"
          />
        </div>

        <div className="grid gap-6 xl:grid-cols-2">
          <Surface variant="glass" className="space-y-3">
            <div className="flex items-center justify-between border-b border-zinc-800/80 pb-3">
              <h2 className="text-sm font-semibold text-zinc-100">Usuarios del tenant</h2>
              <Badge tone="neutral">{users.length}</Badge>
            </div>
            {users.length === 0 ? (
              <p className="text-xs text-zinc-500">La API no devolvió usuarios.</p>
            ) : (
              users.slice(0, 8).map((user) => (
                <div
                  key={user.id}
                  className="flex items-center justify-between rounded-xl border border-zinc-800 bg-zinc-950/60 p-3"
                >
                  <div className="min-w-0">
                    <p className="truncate text-xs font-semibold text-zinc-200">
                      {user.fullName || user.email}
                    </p>
                    <p className="truncate text-[11px] text-zinc-500">{user.email}</p>
                  </div>
                  <Badge tone={user.isActive ? "success" : "neutral"}>
                    {user.isActive ? "Activo" : "Inactivo"}
                  </Badge>
                </div>
              ))
            )}
          </Surface>

          <Surface variant="glass" className="space-y-3" id="sessions">
            <div className="flex items-center justify-between border-b border-zinc-800/80 pb-3">
              <h2 className="text-sm font-semibold text-zinc-100">
                Sesiones y permisos
              </h2>
              <Badge tone="neutral">{permissions.length} permisos</Badge>
            </div>
            {sessions.length === 0 ? (
              <p className="text-xs text-zinc-500">La API no devolvió sesiones.</p>
            ) : (
              sessions.slice(0, 8).map((session) => (
                <div
                  key={session.id}
                  className="flex items-center justify-between rounded-xl border border-zinc-800 bg-zinc-950/60 p-3"
                >
                  <div className="min-w-0">
                    <p className="truncate font-mono text-[11px] text-zinc-300">
                      {session.ipAddress || "IP no reportada"}
                    </p>
                    <p className="text-[10px] text-zinc-500">
                      {new Date(session.loginAtUtc).toLocaleString()}
                    </p>
                  </div>
                  <Badge tone={session.isActive ? "success" : "neutral"}>
                    {session.isActive ? "Activa" : "Cerrada"}
                  </Badge>
                </div>
              ))
            )}
          </Surface>
        </div>
      </div>
    </div>
  );
}
