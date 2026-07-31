"use client";

import React from "react";
import { CheckCircle2, Play, XCircle, Loader2, CircleDashed } from "lucide-react";
import { InstallerStep } from "../api/useInstallerData";
import { Badge, Button, Surface } from "@/shared/components/ui";

function StepStatusBadge({ status }: { status: InstallerStep["status"] }) {
  const tone =
    status === "completed"
      ? "success"
      : status === "error"
        ? "danger"
        : status === "current"
          ? "info"
          : "neutral";
  const label =
    status === "completed"
      ? "Correcto"
      : status === "error"
        ? "Error"
        : status === "current"
          ? "En curso"
          : "Pendiente";
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
    <Surface className="space-y-6">
      <div className="flex flex-col justify-between gap-4 border-b border-neutral-200 pb-4 sm:flex-row sm:items-center">
        <div>
          <h2 className="text-sm font-semibold text-neutral-950">
            Comprobaciones del entorno
          </h2>
          <p className="mt-1 text-xs text-neutral-500">
            Resultados del endpoint de diagnóstico de la API.
          </p>
        </div>
        <Button type="button" onClick={onValidate} loading={isValidating} size="sm">
          {isValidating ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Play className="h-4 w-4" />
          )}
          {isValidating ? "Verificando…" : "Ejecutar diagnóstico"}
        </Button>
      </div>

      <div
        role="status"
        className={`flex items-center gap-3 rounded-md border p-4 text-xs ${
          isSystemReady
            ? "border-neutral-950 bg-neutral-950 text-white"
            : "border-neutral-400 bg-neutral-100 text-neutral-950"
        }`}
      >
        {isSystemReady ? (
          <CheckCircle2 className="h-5 w-5 shrink-0" />
        ) : (
          <XCircle className="h-5 w-5 shrink-0" />
        )}
        <div>
          <p className="font-semibold">
            {isSystemReady
              ? "Entorno listo"
              : "Faltan comprobaciones"}
          </p>
          <p
            className={`mt-0.5 text-[11px] ${
              isSystemReady ? "text-neutral-300" : "text-neutral-500"
            }`}
          >
            Última revisión: {checkedAtLabel}.
          </p>
        </div>
      </div>

      <div className="space-y-3">
        {steps.map((s) => (
          <div
            key={s.id}
            className="flex items-start justify-between gap-4 rounded-md border border-neutral-200 bg-white p-4"
          >
            <div className="flex items-start gap-3">
              <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-md border border-neutral-200 bg-neutral-50 text-neutral-700">
                {s.status === "error" ? (
                  <XCircle className="h-4 w-4" />
                ) : s.status === "completed" ? (
                  <CheckCircle2 className="h-4 w-4" />
                ) : (
                  <CircleDashed className="h-4 w-4" />
                )}
              </div>
              <div>
                <p className="text-xs font-semibold text-neutral-950">
                  {s.title}
                </p>
                <p className="mt-0.5 text-xs text-neutral-500">{s.description}</p>
                <p className="mt-1 font-mono text-[11px] text-neutral-500">
                  {s.details}
                </p>
              </div>
            </div>
            <StepStatusBadge status={s.status} />
          </div>
        ))}
      </div>
    </Surface>
  );
}
