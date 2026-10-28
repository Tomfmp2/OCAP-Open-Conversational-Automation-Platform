import React from "react";
import { Lock, Info } from "lucide-react";
import { Badge, Surface } from "@/shared/components/ui";

export function VaultSecurityStatus() {
  return (
    <Surface variant="glass" glow className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-neutral-200 pb-4">
        <div className="flex items-center gap-2">
          <div className="rounded-xl bg-neutral-100 p-2 text-neutral-700">
            <Lock className="h-4 w-4" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-neutral-950">Vault de credenciales</h2>
            <p className="text-xs text-neutral-500">Información reportada por el servicio de seguridad.</p>
          </div>
        </div>
        <Badge tone="neutral">Sin telemetría</Badge>
      </div>

      <div className="flex items-start gap-3 rounded-xl border border-neutral-200 bg-neutral-50 p-4">
        <Info className="mt-0.5 h-4 w-4 shrink-0 text-neutral-700" />
        <div>
          <p className="text-sm font-medium text-neutral-900">Métricas no disponibles</p>
          <p className="mt-1 text-xs leading-relaxed text-neutral-500">
            La API actual no expone el estado verificable, la rotación de claves ni el número de
            secretos del vault. No se muestran estimaciones.
          </p>
        </div>
      </div>
    </Surface>
  );
}
