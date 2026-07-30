import React from "react";
import { Cpu, Lock, CheckCircle2, RefreshCw } from "lucide-react";
import { AiProviderConfig } from "../api/useIntelligenceData";
import { Surface } from "@/shared/components/ui/Surface";
import { Badge } from "@/shared/components/ui/Badge";
import { Button } from "@/shared/components/ui/Button";

interface ProviderCardProps {
  provider: AiProviderConfig;
  onTest: (id: string) => void;
  isTesting: boolean;
}

export function ProviderCard({ provider, onTest, isTesting }: ProviderCardProps) {
  return (
    <Surface variant="glass" glow={provider.isActive} className="space-y-4">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-500/10 text-blue-500 flex items-center justify-center font-bold text-sm border border-blue-500/20">
            <Cpu className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{provider.displayName}</h3>
              {provider.isEncrypted && (
                <Badge tone="warning">
                  <Lock className="w-2.5 h-2.5" /> AES-256
                </Badge>
              )}
            </div>
            <p className="text-xs text-zinc-400 font-mono">Modelo por defecto: {provider.defaultModel}</p>
          </div>
        </div>

        {provider.isActive ? (
          <Badge tone="success">
            <CheckCircle2 className="w-3.5 h-3.5" /> Activo (Prioridad #{provider.priorityOrder})
          </Badge>
        ) : (
          <Badge tone="neutral">Inactivo</Badge>
        )}
      </div>

      <div className="flex items-center justify-between pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <span className="text-[11px] text-zinc-400 font-mono">
          {provider.lastPingMs > 0 ? `${provider.lastPingMs} ms` : "Latencia no disponible"}
        </span>
        <Button
          type="button"
          variant="secondary"
          size="sm"
          onClick={() => onTest(provider.providerType)}
          loading={isTesting}
        >
          <RefreshCw className="w-3 h-3" /> Probar proveedor
        </Button>
      </div>
    </Surface>
  );
}
