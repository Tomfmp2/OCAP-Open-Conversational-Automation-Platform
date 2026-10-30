"use client";

import React from "react";
import { Plus } from "lucide-react";
import { useAgents } from "@/features/agents/api/useAgentsData";
import { useChannels, type ChannelConnection } from "@/features/channels/api/useChannelsData";
import { ChannelConnectModal } from "@/features/channels/components/ChannelConnectModal";
import { ChannelSettings } from "@/features/channels/components/ChannelSettings";
import { Badge, Button, EmptyState, ErrorState, ListSkeleton, Page, PageHeader } from "@/shared/components/ui";

export default function ChannelsPage() {
  const channels = useChannels();
  const agents = useAgents();
  const [connecting, setConnecting] = React.useState(false);
  const agentName = (id: string | null) => (id ? agents.data?.find((a) => a.id === id)?.name : undefined) ?? "Agente de atención";

  return (
    <Page>
      <PageHeader
        title="Canales"
        description="Por dónde te escriben tus clientes. En cada canal eliges qué agente responde."
        actions={
          <Button onClick={() => setConnecting(true)}>
            <Plus className="h-4 w-4" aria-hidden /> Conectar canal
          </Button>
        }
      />

      {channels.isLoading ? (
        <ListSkeleton label="Cargando canales" />
      ) : channels.isError ? (
        <ErrorState message={channels.error instanceof Error ? channels.error.message : undefined} onRetry={() => void channels.refetch()} />
      ) : !channels.data?.length ? (
        <EmptyState
          title="Todavía no conectaste ningún canal"
          description="Conecta WhatsApp, un bot de Telegram o un chat para tu sitio web."
          action={<Button onClick={() => setConnecting(true)}>Conectar canal</Button>}
        />
      ) : (
        <ul className="divide-y divide-neutral-200 rounded-lg border border-neutral-200 bg-white">
          {channels.data.map((channel) => (
            <ChannelRow key={channel.id} channel={channel} responder={channel.widget ? undefined : agentName(channel.agentId)} />
          ))}
        </ul>
      )}

      <ChannelConnectModal open={connecting} onClose={() => setConnecting(false)} />
    </Page>
  );
}

function ChannelRow({ channel, responder }: { channel: ChannelConnection; responder?: string }) {
  const [open, setOpen] = React.useState(false);

  return (
    <li className="flex items-center gap-4 px-4 py-3 sm:px-5">
      <div className="min-w-0 flex-1">
        <p className="flex flex-wrap items-center gap-2">
          <span className="truncate text-sm font-medium text-neutral-950">{channel.name}</span>
          {channel.status === "Pending" && <Badge tone="warning">Falta escanear el QR</Badge>}
          {channel.power === "off" ? (
            <Badge>Apagado</Badge>
          ) : channel.power === "stopping" ? (
            <Badge tone="warning">Apagándose</Badge>
          ) : (
            channel.status === "Disconnected" && <Badge tone="danger">Desconectado</Badge>
          )}
        </p>
        <p className="truncate text-sm text-neutral-600">
          {channel.provider}
          {channel.accountIdentifier && `, ${channel.accountIdentifier}`}
          {responder && `. Responde: ${responder}`}
        </p>
      </div>
      <Button variant="secondary" size="sm" onClick={() => setOpen(true)} aria-label={`Configurar ${channel.name}`}>
        Configurar
      </Button>
      <ChannelSettings channel={channel} open={open} onClose={() => setOpen(false)} />
    </li>
  );
}
