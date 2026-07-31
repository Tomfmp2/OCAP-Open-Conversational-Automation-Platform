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
    <Surface className="space-y-4">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-md border border-neutral-200 bg-neutral-50 text-neutral-800">
            <Cpu className="h-5 w-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-neutral-950">
                {provider.displayName}
              </h3>
              {provider.isEncrypted && (
                <Badge tone="warning">
                  <Lock className="h-2.5 w-2.5" /> Cifrado
                </Badge>
              )}
            </div>
            <p className="font-mono text-xs text-neutral-500">
              Modelo: {provider.defaultModel}
            </p>
          </div>
        </div>

        {provider.isActive ? (
          <Badge tone="success">
            <CheckCircle2 className="h-3.5 w-3.5" /> Activo
          </Badge>
        ) : (
          <Badge tone="neutral">Inactivo</Badge>
        )}
      </div>

      <div className="flex items-center justify-between border-t border-neutral-100 pt-2">
        <span className="font-mono text-[11px] text-neutral-500">
          {provider.lastPingMs > 0
            ? `${provider.lastPingMs} ms`
            : "Latencia no disponible"}
        </span>
        <Button
          type="button"
          variant="secondary"
          size="sm"
          onClick={() => onTest(provider.providerType)}
          loading={isTesting}
        >
          <RefreshCw className="h-3 w-3" /> Probar proveedor
        </Button>
      </div>
    </Surface>
  );
}
