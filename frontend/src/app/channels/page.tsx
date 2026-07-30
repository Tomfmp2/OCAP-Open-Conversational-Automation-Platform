"use client";

import React from "react";
import { MessageSquare, Plus, RefreshCw, Radio, CheckCircle2 } from "lucide-react";
import { useChannelsData, ChannelConnectionDto } from "@/features/channels/api/useChannelsData";
import { ChannelCard } from "@/features/channels/components/ChannelCard";
import { ChannelConnectModal } from "@/features/channels/components/ChannelConnectModal";
import { ChannelsSkeleton } from "@/features/channels/components/ChannelsSkeleton";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Button } from "@/shared/components/ui/Button";
import { MetricCard } from "@/shared/components/ui/MetricCard";
import { EmptyState } from "@/shared/components/ui/EmptyState";
import { ErrorState } from "@/shared/components/ui/ErrorState";

export default function ChannelsPage() {
  const {
    data: channels,
    isLoading,
    isError,
    error,
    refetch,
    isFetching,
    testConnectionMutation,
  } = useChannelsData();
  const [modalOpen, setModalOpen] = React.useState(false);
  const [testingId, setTestingId] = React.useState<string | null>(null);

  if (isLoading) {
    return <ChannelsSkeleton />;
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

  const handleTest = (id: string) => {
    setTestingId(id);
    testConnectionMutation.mutate(id, {
      onSettled: () => setTestingId(null),
    });
  };

  const channelList = channels || [];
  const connectedCount = channelList.filter((c) =>
    ["connected", "online"].includes(c.status.toLowerCase())
  ).length;

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Canales"
        description="Conexiones reales entre OCAP y tus canales de entrada."
        icon={<Radio className="h-5 w-5 text-blue-500" />}
        actions={
          <>
            <Button variant="secondary" size="sm" onClick={() => void refetch()} loading={isFetching}>
              <RefreshCw className="h-3.5 w-3.5" /> Actualizar
            </Button>
            <Button size="sm" onClick={() => setModalOpen(true)}>
              <Plus className="h-3.5 w-3.5" /> Conectar canal
            </Button>
          </>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2">
        <MetricCard title="Canales configurados" value={channelList.length} icon={MessageSquare} tone="info" />
        <MetricCard title="Conexiones activas" value={connectedCount} icon={CheckCircle2} tone="success" />
      </div>

      {channelList.length === 0 ? (
        <EmptyState
          title="No hay canales configurados"
          description="Conecta un proveedor cuando tengas sus credenciales disponibles."
          action={<Button size="sm" onClick={() => setModalOpen(true)}><Plus className="h-4 w-4" /> Conectar canal</Button>}
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {channelList.map((channel: ChannelConnectionDto) => (
            <ChannelCard
              key={channel.id}
              channel={channel}
              onTest={handleTest}
              isTesting={testingId === channel.id}
            />
          ))}
        </div>
      )}

      <ChannelConnectModal open={modalOpen} onClose={() => setModalOpen(false)} />
    </div>
  );
}
