"use client";

import React from "react";
import { ShieldCheck, RefreshCw, Inbox } from "lucide-react";
import { useSecurityData } from "@/features/security/api/useSecurityData";
import { RbacMatrixTable } from "@/features/security/components/RbacMatrixTable";
import { VaultSecurityStatus } from "@/features/security/components/VaultSecurityStatus";
import { SecuritySkeleton } from "@/features/security/components/SecuritySkeleton";

export default function SecurityPage() {
  const { data, isLoading, refetch, isFetching } = useSecurityData();

  if (isLoading) {
    return <SecuritySkeleton />;
  }

  const { roles, vault } = data || { roles: [], vault: { algorithm: "AES-256", keyRotationDays: 30, totalSecretsEncrypted: 0, status: "healthy" as const } };

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <ShieldCheck className="w-5 h-5 text-blue-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Centro de Seguridad, RBAC & Credential Vault
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Administración de permisos multi-tenant, matriz de roles y cifrado AES-256 de credenciales.
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

      <div className="grid grid-cols-1 gap-6">
        <VaultSecurityStatus vault={vault} />
        {roles.length === 0 ? (
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-8 text-center space-y-2">
            <Inbox className="w-6 h-6 text-zinc-400 mx-auto" />
            <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">No hay roles ni permisos asignados</h3>
            <p className="text-xs text-zinc-500">Configura la matriz de acceso RBAC para el tenant activo.</p>
          </div>
        ) : (
          <RbacMatrixTable roles={roles} />
        )}
      </div>
    </div>
  );
}
