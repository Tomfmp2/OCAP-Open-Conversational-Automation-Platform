import React from "react";
import { Lock, Info } from "lucide-react";
import { Badge, Surface } from "@/shared/components/ui";

export function VaultSecurityStatus() {
  return (
    <Surface variant="glass" glow className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800/80 pb-4">
        <div className="flex items-center gap-2">
          <div className="rounded-xl bg-violet-500/10 p-2 text-violet-400">
            <Lock className="h-4 w-4" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-zinc-100">Vault de credenciales</h2>
            <p className="text-[11px] text-zinc-500">Información reportada por el servicio de seguridad.</p>
          </div>
        </div>
        <Badge tone="neutral">Sin telemetría</Badge>
      </div>

      <div className="flex items-start gap-3 rounded-xl border border-blue-500/20 bg-blue-500/5 p-4">
        <Info className="mt-0.5 h-4 w-4 shrink-0 text-blue-400" />
        <div>
          <p className="text-sm font-medium text-zinc-200">Métricas no disponibles</p>
          <p className="mt-1 text-xs leading-relaxed text-zinc-500">
            La API actual no expone el estado verificable, la rotación de claves ni el número de
            secretos del vault. No se muestran estimaciones.
          </p>
        </div>
      </div>
    </Surface>
  );
}
