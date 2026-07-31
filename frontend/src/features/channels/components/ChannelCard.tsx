import React from "react";
import { MessageSquare, CheckCircle2, RefreshCw, Power } from "lucide-react";
import { ChannelConnection } from "../api/useChannelsData";
import { Surface } from "@/shared/components/ui/Surface";
import { Badge } from "@/shared/components/ui/Badge";
import { Button } from "@/shared/components/ui/Button";

interface ChannelCardProps {
  channel: ChannelConnection;
  onTest: (id: string) => void;
  isTesting: boolean;
}

export function ChannelCard({ channel, onTest, isTesting }: ChannelCardProps) {
  const isConnected = ["connected", "online"].includes(channel.status.toLowerCase());

  return (
    <Surface className="space-y-4">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-md border border-neutral-200 bg-neutral-50 text-neutral-800">
            <MessageSquare className="h-5 w-5" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-neutral-950">{channel.name}</h3>
            <p className="font-mono text-xs text-neutral-500">{channel.accountIdentifier}</p>
            <p className="mt-0.5 text-[11px] text-neutral-500">{channel.provider}</p>
          </div>
        </div>

        {isConnected ? (
          <Badge tone="success">
            <CheckCircle2 className="h-3.5 w-3.5" /> Conectado
          </Badge>
        ) : (
          <Badge tone="neutral">
            <Power className="h-3.5 w-3.5" /> Desconectado
          </Badge>
        )}
      </div>

      <div className="flex items-center justify-between border-t border-neutral-100 pt-2">
        <span className="font-mono text-[11px] text-neutral-500">
          {channel.latencyMs > 0
            ? `Latencia: ${channel.latencyMs} ms`
            : "Latencia no disponible"}
        </span>
        <Button
          type="button"
          variant="secondary"
          size="sm"
          onClick={() => onTest(channel.id)}
          loading={isTesting}
          disabled={!isConnected}
        >
          <RefreshCw className="h-3 w-3" /> Probar conexión
        </Button>
      </div>
    </Surface>
  );
}
