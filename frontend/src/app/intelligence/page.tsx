"use client";

import React from "react";
import { Cpu, Plus, RefreshCw, Lock, Zap, Inbox } from "lucide-react";
import { useIntelligenceData, AiProviderConfigDto } from "@/features/intelligence/api/useIntelligenceData";
import { ProviderCard } from "@/features/intelligence/components/ProviderCard";
import { AddProviderModal } from "@/features/intelligence/components/AddProviderModal";
import { IntelligenceSkeleton } from "@/features/intelligence/components/IntelligenceSkeleton";

export default function IntelligencePage() {
  const { data: providers, isLoading, refetch, isFetching, testProviderMutation } = useIntelligenceData();
  const [modalOpen, setModalOpen] = React.useState(false);
  const [testingId, setTestingId] = React.useState<string | null>(null);

  if (isLoading) {
    return <IntelligenceSkeleton />;
  }

  const handleTest = (id: string) => {
    setTestingId(id);
    testProviderMutation.mutate(id, {
      onSettled: () => setTestingId(null),
    });
  };

  const providerList = providers || [];
  const activeCount = providerList.filter((p) => p.isActive).length;
  const totalTokens = providerList.reduce((acc, curr) => acc + (curr.totalTokensProcessed || 0), 0);

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <Cpu className="w-5 h-5 text-blue-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Centro de IA, Modelos & Credential Vault (CAP-04)
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Administra proveedores de lenguaje (OpenAI, Gemini, Ollama, Local) aislados del orquestador de agentes.
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
          <button
            onClick={() => setModalOpen(true)}
            className="flex items-center gap-2 px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Registrar Proveedor</span>
          </button>
        </div>
      </div>

      {/* KPI strip */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 flex items-center justify-between shadow-sm">
          <div>
            <p className="text-xs text-zinc-500">Proveedores Activos</p>
            <p className="text-2xl font-bold text-zinc-900 dark:text-zinc-100 mt-1">{activeCount} / {providerList.length}</p>
          </div>
          <div className="p-2.5 rounded-xl bg-blue-50 dark:bg-blue-950/40 text-blue-500">
            <Cpu className="w-5 h-5" />
          </div>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 flex items-center justify-between shadow-sm">
          <div>
            <p className="text-xs text-zinc-500">Total Tokens Procesados</p>
            <p className="text-2xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">
              {totalTokens > 0 ? `${(totalTokens / 1000000).toFixed(2)}M` : "0"}
            </p>
          </div>
          <div className="p-2.5 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 text-emerald-500">
            <Zap className="w-5 h-5" />
          </div>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 flex items-center justify-between shadow-sm">
          <div>
            <p className="text-xs text-zinc-500">Seguridad Vault</p>
            <p className="text-xs font-bold text-amber-500 mt-1">AES-256 Multi-tenant</p>
          </div>
          <div className="p-2.5 rounded-xl bg-amber-50 dark:bg-amber-950/40 text-amber-500">
            <Lock className="w-5 h-5" />
          </div>
        </div>
      </div>

      {/* Provider Cards List or Empty State */}
      {providerList.length === 0 ? (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-12 text-center space-y-4">
          <div className="w-12 h-12 rounded-full bg-zinc-100 dark:bg-zinc-800 text-zinc-400 mx-auto flex items-center justify-center">
            <Inbox className="w-6 h-6" />
          </div>
          <div>
            <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-100">No existen proveedores de IA configurados</h3>
            <p className="text-xs text-zinc-500 mt-1">
              Registra credenciales de OpenAI, Gemini u Ollama para habilitar la orquestación de inteligencia.
            </p>
          </div>
          <button
            onClick={() => setModalOpen(true)}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors"
          >
            <Plus className="w-4 h-4" />
            <span>Registrar Proveedor</span>
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {providerList.map((provider: AiProviderConfigDto) => (
            <ProviderCard
              key={provider.id}
              provider={provider}
              onTest={handleTest}
              isTesting={testingId === provider.id}
            />
          ))}
        </div>
      )}

      <AddProviderModal open={modalOpen} onClose={() => setModalOpen(false)} />
    </div>
  );
}
