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
  const getProviderColor = (provider: string) => {
    switch (provider) {
      case "Telegram":
        return "bg-sky-500/10 text-sky-500 border-sky-500/20";
      case "WhatsApp":
        return "bg-emerald-500/10 text-emerald-500 border-emerald-500/20";
      case "Google":
        return "bg-amber-500/10 text-amber-500 border-amber-500/20";
      default:
        return "bg-purple-500/10 text-purple-500 border-purple-500/20";
    }
  };

  return (
    <Surface variant="glass" glow={isConnected} className="space-y-4">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className={`w-10 h-10 rounded-xl flex items-center justify-center font-bold text-sm border ${getProviderColor(channel.provider)}`}>
            <MessageSquare className="w-5 h-5" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{channel.name}</h3>
            <p className="text-xs text-zinc-400 font-mono">{channel.accountIdentifier}</p>
          </div>
        </div>

        {isConnected ? (
          <Badge tone="success">
            <CheckCircle2 className="w-3.5 h-3.5" /> Conectado
          </Badge>
        ) : (
          <Badge tone="neutral">
            <Power className="w-3.5 h-3.5" /> Desconectado
          </Badge>
        )}
      </div>

      <div className="flex items-center justify-between pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <span className="text-[11px] text-zinc-400 font-mono">
          {channel.latencyMs > 0 ? `Latencia: ${channel.latencyMs} ms` : "Latencia no disponible"}
        </span>
        <Button
          type="button"
          variant="secondary"
          size="sm"
          onClick={() => onTest(channel.id)}
          loading={isTesting}
          disabled={!isConnected}
        >
          <RefreshCw className="w-3 h-3" /> Probar conexión
        </Button>
      </div>
    </Surface>
  );
}
