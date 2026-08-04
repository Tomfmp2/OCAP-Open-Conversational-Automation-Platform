"use client";

import React from "react";
import {
  CheckCircle2,
  Cpu,
  Lock,
  Pencil,
  Play,
  Power,
  RefreshCw,
  Star,
  Trash2,
} from "lucide-react";
import type { TenantProviderConfig } from "../api/useIntelligenceData";
import { Surface } from "@/shared/components/ui/Surface";
import { Badge } from "@/shared/components/ui/Badge";
import { Button } from "@/shared/components/ui/Button";

interface ProviderCardProps {
  provider: TenantProviderConfig;
  onTest: (providerName: string) => void;
  onEdit: (provider: TenantProviderConfig) => void;
  onToggle: (provider: TenantProviderConfig) => void;
  onSelect: (provider: TenantProviderConfig) => void;
  onDelete: (provider: TenantProviderConfig) => void;
  isTesting: boolean;
  isToggling: boolean;
  isSelecting: boolean;
  isDeleting: boolean;
  testResult?: string | null;
}

export function ProviderCard({
  provider,
  onTest,
  onEdit,
  onToggle,
  onSelect,
  onDelete,
  isTesting,
  isToggling,
  isSelecting,
  isDeleting,
  testResult,
}: ProviderCardProps) {
  const healthTone =
    provider.healthStatus === "Healthy"
      ? "success"
      : provider.healthStatus === "Unhealthy"
        ? "warning"
        : "neutral";

  const isCatalogOnly = provider.id.startsWith("catalog-");

  return (
    <Surface className="space-y-4">
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-md border border-neutral-200 bg-neutral-50 text-neutral-800">
            <Cpu className="h-5 w-5" />
          </div>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="truncate text-sm font-semibold text-neutral-950">
                {provider.displayName || provider.providerName}
              </h3>
              {provider.hasVaultKey && (
                <Badge tone="neutral">
                  <Lock className="h-2.5 w-2.5" /> Vault
                </Badge>
              )}
              {provider.isRuntimeActive && (
                <Badge tone="info">
                  <Star className="h-2.5 w-2.5" /> Preferido
                </Badge>
              )}
              {isCatalogOnly && (
                <Badge tone="warning">Sin config tenant</Badge>
              )}
            </div>
            <p className="mt-0.5 font-mono text-xs text-neutral-500">
              {provider.providerName} · {provider.modelName || "sin modelo"}
            </p>
            {provider.baseUrl && (
              <p className="mt-0.5 truncate font-mono text-[11px] text-neutral-400">
                {provider.baseUrl}
              </p>
            )}
          </div>
        </div>

        <div className="flex shrink-0 flex-col items-end gap-1.5">
          {isCatalogOnly ? (
            <Badge tone="neutral">Solo runtime</Badge>
          ) : provider.isEnabled ? (
            <Badge tone="success">
              <CheckCircle2 className="h-3.5 w-3.5" /> Habilitado
            </Badge>
          ) : (
            <Badge tone="neutral">Deshabilitado</Badge>
          )}
          <Badge tone={healthTone}>{provider.healthStatus}</Badge>
        </div>
      </div>

      {isCatalogOnly && (
        <p className="text-[11px] leading-relaxed text-neutral-500">
          Viene del registry/.env. Pulsa <strong>Registrar</strong> para poder editar modelo,
          API key y activarlo por tenant.
        </p>
      )}

      {testResult && (
        <p className="rounded-md border border-neutral-200 bg-neutral-50 px-3 py-2 font-mono text-[11px] text-neutral-700">
          {testResult}
        </p>
      )}

      <div className="flex flex-wrap items-center justify-between gap-2 border-t border-neutral-100 pt-3">
        <span className="font-mono text-[11px] text-neutral-500">
          {provider.lastPingMs > 0
            ? `${provider.lastPingMs} ms`
            : "Sin latencia"}
        </span>
        <div className="flex flex-wrap justify-end gap-1.5">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => onTest(provider.providerName)}
            loading={isTesting}
          >
            <RefreshCw className="h-3 w-3" /> Probar
          </Button>
          {!isCatalogOnly && (
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => onSelect(provider)}
              loading={isSelecting}
              disabled={!provider.isEnabled || provider.isRuntimeActive}
            >
              <Play className="h-3 w-3" /> Usar
            </Button>
          )}
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => onEdit(provider)}
          >
            <Pencil className="h-3 w-3" />
            {isCatalogOnly ? "Registrar" : "Editar"}
          </Button>
          {!isCatalogOnly && (
            <>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => onToggle(provider)}
                loading={isToggling}
              >
                <Power className="h-3 w-3" />
                {provider.isEnabled ? "Desactivar" : "Activar"}
              </Button>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => onDelete(provider)}
                loading={isDeleting}
              >
                <Trash2 className="h-3 w-3" />
              </Button>
            </>
          )}
        </div>
      </div>
    </Surface>
  );
}
