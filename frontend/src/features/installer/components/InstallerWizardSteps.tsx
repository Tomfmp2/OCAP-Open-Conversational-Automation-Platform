"use client";

import React from "react";
import { CheckCircle2, Play, XCircle, Loader2, CircleDashed } from "lucide-react";
import { InstallerStep } from "../api/useInstallerData";
import { Badge, Button, Surface } from "@/shared/components/ui";

function StepStatusBadge({ status }: { status: InstallerStep["status"] }) {
  const tone = status === "completed" ? "success" : status === "error" ? "danger" : status === "current" ? "info" : "neutral";
  const label = status === "completed" ? "Correcto" : status === "error" ? "Error" : status === "current" ? "En curso" : "Pendiente";
  return <Badge tone={tone}>{label}</Badge>;
}

interface InstallerWizardStepsProps {
  steps: InstallerStep[];
  isSystemReady: boolean;
  isValidating: boolean;
  lastCheckedAt: string;
  onValidate: () => void;
}

export function InstallerWizardSteps({
  steps,
  isSystemReady,
  isValidating,
  lastCheckedAt,
  onValidate,
}: InstallerWizardStepsProps) {
  const checkedAt = new Date(lastCheckedAt);
  const checkedAtLabel = Number.isNaN(checkedAt.getTime())
    ? "hora no disponible"
    : checkedAt.toLocaleString();

  return (
    <Surface variant="glass" glow className="space-y-6">
      <div className="flex flex-col justify-between gap-4 border-b border-zinc-800/80 pb-4 sm:flex-row sm:items-center">
        <div>
          <h2 className="text-sm font-semibold text-zinc-100">Comprobaciones del entorno</h2>
          <p className="mt-1 text-xs text-zinc-500">Resultados devueltos por el endpoint de diagnóstico.</p>
        </div>
        <Button
          type="button"
          onClick={onValidate}
          loading={isValidating}
          size="sm"
        >
          {isValidating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Play className="h-4 w-4" />}
          {isValidating ? "Verificando..." : "Ejecutar diagnóstico"}
        </Button>
      </div>

      <div
        role="status"
        className={`flex items-center gap-3 rounded-xl border p-4 text-xs ${
          isSystemReady
            ? "bg-emerald-500/10 border-emerald-500/20 text-emerald-600 dark:text-emerald-400"
            : "bg-red-500/10 border-red-500/20 text-red-600 dark:text-red-400"
        }`}
      >
        {isSystemReady ? (
          <CheckCircle2 className="w-5 h-5 shrink-0" />
        ) : (
          <XCircle className="w-5 h-5 shrink-0" />
        )}
        <div>
          <p className="font-bold">
            {isSystemReady ? "Plataforma OCAP operacional" : "Plataforma OCAP no preparada"}
          </p>
          <p className="text-[11px] text-zinc-400 mt-0.5">
            Estado agregado reportado por el diagnóstico. Última revisión: {checkedAtLabel}.
          </p>
        </div>
      </div>

      <div className="space-y-4">
        {steps.map((s) => (
          <div
            key={s.id}
            className="flex items-start justify-between gap-4 rounded-xl border border-zinc-800 bg-zinc-950/70 p-4"
          >
            <div className="flex items-start gap-3">
              <div
                className={`w-8 h-8 rounded-lg flex items-center justify-center font-bold text-xs shrink-0 mt-0.5 ${
                  s.status === "completed"
                    ? "bg-emerald-500/10 text-emerald-500"
                    : s.status === "error"
                      ? "bg-red-500/10 text-red-500"
                      : "bg-zinc-500/10 text-zinc-500"
                }`}
              >
                {s.status === "error" ? (
                  <XCircle className="h-4 w-4" />
                ) : s.status === "completed" ? (
                  <CheckCircle2 className="h-4 w-4" />
                ) : (
                  <CircleDashed className="h-4 w-4" />
                )}
              </div>
              <div>
                <p className="text-xs font-bold text-zinc-900 dark:text-zinc-100">
                  Paso #{s.id}: {s.title}
                </p>
                <p className="text-xs text-zinc-500 mt-0.5">{s.description}</p>
                <p className="text-[11px] text-zinc-400 font-mono mt-1">{s.details}</p>
              </div>
            </div>
            <StepStatusBadge status={s.status} />
          </div>
        ))}
      </div>
    </Surface>
  );
}
