"use client";

import React from "react";
import { MessageSquare, Plus, RefreshCw, Radio, CheckCircle2, ShieldCheck, Inbox } from "lucide-react";
import { useChannelsData } from "@/features/channels/api/useChannelsData";
import { ChannelCard } from "@/features/channels/components/ChannelCard";
import { ChannelConnectModal } from "@/features/channels/components/ChannelConnectModal";
import { ChannelsSkeleton } from "@/features/channels/components/ChannelsSkeleton";

export default function ChannelsPage() {
  const { data: channels, isLoading, isError, refetch, isFetching, testConnectionMutation } = useChannelsData();
  const [modalOpen, setModalOpen] = React.useState(false);
  const [testingId, setTestingId] = React.useState<string | null>(null);

  if (isLoading) {
    return <ChannelsSkeleton />;
  }

  const handleTest = (id: string) => {
    setTestingId(id);
    testConnectionMutation.mutate(id, {
      onSettled: () => setTestingId(null),
    });
  };

  const channelList = channels || [];
  const connectedCount = channelList.filter((c) => c.status === "connected" || c.status === "Online").length;

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <Radio className="w-5 h-5 text-blue-500 animate-pulse" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Gestión de Canales Omnicanal (Channels Hub)
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Administra los adaptadores de entrada y salida aislados del backend hexagonal de OCAP.
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
            <span>Conectar Nuevo Canal</span>
          </button>
        </div>
      </div>

      {/* Summary KPI Strip */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 flex items-center justify-between shadow-sm">
          <div>
            <p className="text-xs text-zinc-500">Canales Configurados</p>
            <p className="text-2xl font-bold text-zinc-900 dark:text-zinc-100 mt-1">{channelList.length}</p>
          </div>
          <div className="p-2.5 rounded-xl bg-blue-50 dark:bg-blue-950/40 text-blue-500">
            <MessageSquare className="w-5 h-5" />
          </div>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 flex items-center justify-between shadow-sm">
          <div>
            <p className="text-xs text-zinc-500">Conexiones Activas</p>
            <p className="text-2xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">{connectedCount} Activas</p>
          </div>
          <div className="p-2.5 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 text-emerald-500">
            <CheckCircle2 className="w-5 h-5" />
          </div>
        </div>

        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-4 flex items-center justify-between shadow-sm">
          <div>
            <p className="text-xs text-zinc-500">Aislamiento Hexagonal</p>
            <p className="text-xs font-bold text-zinc-900 dark:text-zinc-100 mt-1">Decoupled Architecture</p>
          </div>
          <div className="p-2.5 rounded-xl bg-purple-50 dark:bg-purple-950/40 text-purple-500">
            <ShieldCheck className="w-5 h-5" />
          </div>
        </div>
      </div>

      {/* Channels List or Empty State */}
      {channelList.length === 0 ? (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-12 text-center space-y-4">
          <div className="w-12 h-12 rounded-full bg-zinc-100 dark:bg-zinc-800 text-zinc-400 mx-auto flex items-center justify-center">
            <Inbox className="w-6 h-6" />
          </div>
          <div>
            <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-100">No existen canales conectados</h3>
            <p className="text-xs text-zinc-500 mt-1">
              Conecta un adaptador de Telegram, WhatsApp o Google Workspace para comenzar a recibir eventos.
            </p>
          </div>
          <button
            onClick={() => setModalOpen(true)}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors"
          >
            <Plus className="w-4 h-4" />
            <span>Conectar Canal</span>
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {channelList.map((channel: any) => (
            <ChannelCard
              key={channel.id}
              channel={channel}
              onTest={handleTest}
              isTesting={testingId === channel.id}
            />
          ))}
        </div>
      )}

      {/* Modal */}
      <ChannelConnectModal open={modalOpen} onClose={() => setModalOpen(false)} />
    </div>
  );
}
