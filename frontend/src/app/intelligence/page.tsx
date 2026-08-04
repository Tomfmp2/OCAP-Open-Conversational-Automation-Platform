"use client";

import React from "react";
import { Activity, Cpu, Plus, RefreshCw } from "lucide-react";
import {
  useIntelligenceData,
  type TenantProviderConfig,
} from "@/features/intelligence/api/useIntelligenceData";
import { ProviderCard } from "@/features/intelligence/components/ProviderCard";
import { ProviderFormModal } from "@/features/intelligence/components/ProviderFormModal";
import { IntelligenceSkeleton } from "@/features/intelligence/components/IntelligenceSkeleton";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Button } from "@/shared/components/ui/Button";
import { MetricCard } from "@/shared/components/ui/MetricCard";
import { EmptyState } from "@/shared/components/ui/EmptyState";
import { ErrorState } from "@/shared/components/ui/ErrorState";
import { Surface } from "@/shared/components/ui/Surface";

export default function IntelligencePage() {
  const {
    providers,
    runtime,
    modelsByProvider,
    isLoading,
    isError,
    error,
    refetch,
    isFetching,
    testProviderMutation,
    setStatusMutation,
    selectProviderMutation,
    deleteProviderMutation,
    migrateObsoleteModelMutation,
  } = useIntelligenceData();

  const [modalOpen, setModalOpen] = React.useState(false);
  const [editing, setEditing] = React.useState<TenantProviderConfig | null>(null);
  const [createPrefill, setCreatePrefill] = React.useState<{
    providerName: string;
    modelName?: string;
  } | null>(null);
  const [testingName, setTestingName] = React.useState<string | null>(null);
  const [testResults, setTestResults] = React.useState<Record<string, string>>({});
  const [busyId, setBusyId] = React.useState<string | null>(null);
  const [pageError, setPageError] = React.useState<string | null>(null);

  React.useEffect(() => {
    const obsolete = providers.filter(
      (p) =>
        !p.id.startsWith("catalog-") &&
        /gemini.*1\.5|gemini-2\.0-flash(?!-lite)/i.test(p.modelName)
    );
    for (const p of obsolete) {
      void migrateObsoleteModelMutation.mutateAsync(p).catch(() => undefined);
    }
  }, [providers]); // eslint-disable-line react-hooks/exhaustive-deps

  if (isLoading) {
    return <IntelligenceSkeleton />;
  }

  if (isError) {
    return (
      <div className="mx-auto max-w-7xl">
        <ErrorState
          message={error instanceof Error ? error.message : undefined}
          onRetry={() => void refetch()}
        />
      </div>
    );
  }

  const enabledCount = providers.filter((p) => p.isEnabled).length;

  const handleTest = (providerName: string) => {
    setPageError(null);
    setTestingName(providerName);
    testProviderMutation.mutate(providerName, {
      onSuccess: (res) => {
        setTestResults((prev) => ({
          ...prev,
          [providerName]: `OK · ${res.modelUsed} · ${Math.round(res.latencyMs)} ms · ${res.tokensUsed} tokens`,
        }));
      },
      onError: (err) => {
        const detail =
          err &&
          typeof err === "object" &&
          "body" in err &&
          err.body &&
          typeof err.body === "object" &&
          "message" in err.body &&
          typeof (err.body as { message: unknown }).message === "string"
            ? (err.body as { message: string }).message
            : err instanceof Error
              ? err.message
              : "Prueba fallida";
        setTestResults((prev) => ({
          ...prev,
          [providerName]: detail,
        }));
        setPageError(detail);
      },
      onSettled: () => setTestingName(null),
    });
  };

  const handleToggle = (provider: TenantProviderConfig) => {
    setPageError(null);
    setBusyId(provider.id);
    setStatusMutation.mutate(
      { id: provider.id, enable: !provider.isEnabled },
      {
        onError: (err) =>
          setPageError(err instanceof Error ? err.message : "No se pudo cambiar el estado"),
        onSettled: () => setBusyId(null),
      }
    );
  };

  const handleSelect = (provider: TenantProviderConfig) => {
    setPageError(null);
    setBusyId(provider.id);
    selectProviderMutation.mutate(provider.providerName, {
      onError: (err) =>
        setPageError(err instanceof Error ? err.message : "No se pudo seleccionar"),
      onSettled: () => setBusyId(null),
    });
  };

  const handleDelete = (provider: TenantProviderConfig) => {
    if (
      !window.confirm(
        `¿Eliminar «${provider.displayName || provider.providerName}»? Se borrará también la key del vault.`
      )
    ) {
      return;
    }
    setPageError(null);
    setBusyId(provider.id);
    deleteProviderMutation.mutate(provider.id, {
      onError: (err) =>
        setPageError(err instanceof Error ? err.message : "No se pudo eliminar"),
      onSettled: () => setBusyId(null),
    });
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="IA y modelos"
        description="Gestiona proveedores del tenant: modelo, API key, activar/desactivar y proveedor preferido en runtime."
        icon={<Cpu className="h-5 w-5 text-neutral-700" />}
        actions={
          <>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => void refetch()}
              loading={isFetching}
            >
              <RefreshCw className="h-3.5 w-3.5" /> Actualizar
            </Button>
            <Button
              size="sm"
              onClick={() => {
                setEditing(null);
                setCreatePrefill(null);
                setModalOpen(true);
              }}
            >
              <Plus className="h-3.5 w-3.5" /> Registrar proveedor
            </Button>
          </>
        }
      />

      {runtime && (
        <Surface className="flex flex-wrap items-center justify-between gap-3 px-4 py-3 text-sm">
          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-neutral-500">
              Runtime activo
            </p>
            <p className="mt-0.5 font-mono text-sm text-neutral-950">
              {runtime.activeProvider} / {runtime.activeModel}
              <span className="ml-2 text-xs text-neutral-500">({runtime.status})</span>
            </p>
          </div>
        </Surface>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <MetricCard
          title="Proveedores configurados"
          value={providers.length}
          icon={Cpu}
          tone="accent"
        />
        <MetricCard
          title="Habilitados"
          value={enabledCount}
          icon={Activity}
          tone="success"
        />
      </div>

      {pageError && (
        <p className="rounded-md border-2 border-neutral-950 bg-white px-3 py-2 text-xs text-neutral-950">
          {pageError}
        </p>
      )}

      {providers.length === 0 ? (
        <EmptyState
          title="No hay proveedores configurados"
          description="Registra Gemini, OpenAI, Claude u Ollama con modelo y API key."
          action={
            <Button
              size="sm"
              onClick={() => {
                setEditing(null);
                setCreatePrefill(null);
                setModalOpen(true);
              }}
            >
              <Plus className="h-4 w-4" /> Registrar proveedor
            </Button>
          }
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {providers.map((provider) => (
            <ProviderCard
              key={provider.id}
              provider={provider}
              onTest={handleTest}
              onEdit={(p) => {
                if (p.id.startsWith("catalog-")) {
                  setEditing(null);
                  setCreatePrefill({
                    providerName: p.providerName,
                    modelName:
                      p.modelName !== "default" ? p.modelName : undefined,
                  });
                  setModalOpen(true);
                } else {
                  setCreatePrefill(null);
                  setEditing(p);
                  setModalOpen(true);
                }
              }}
              onToggle={handleToggle}
              onSelect={handleSelect}
              onDelete={handleDelete}
              isTesting={testingName === provider.providerName}
              isToggling={
                busyId === provider.id && setStatusMutation.isPending
              }
              isSelecting={
                busyId === provider.id && selectProviderMutation.isPending
              }
              isDeleting={
                busyId === provider.id && deleteProviderMutation.isPending
              }
              testResult={testResults[provider.providerName] ?? null}
            />
          ))}
        </div>
      )}

      <ProviderFormModal
        open={modalOpen}
        onClose={() => {
          setModalOpen(false);
          setEditing(null);
          setCreatePrefill(null);
        }}
        editing={editing}
        createPrefill={createPrefill}
        suggestedModels={modelsByProvider}
      />
    </div>
  );
}
