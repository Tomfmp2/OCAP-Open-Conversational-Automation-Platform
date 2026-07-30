"use client";

import React from "react";
import { CheckCircle2, Play, XCircle, Loader2 } from "lucide-react";
import { InstallerStep } from "../api/useInstallerData";

function StepStatusBadge({ status }: { status: InstallerStep["status"] }) {
  if (status === "completed") {
    return (
      <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">
        OK
      </span>
    );
  }
  if (status === "error") {
    return (
      <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded bg-red-500/10 text-red-500 border border-red-500/20">
        FAIL
      </span>
    );
  }
  return (
    <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded bg-zinc-500/10 text-zinc-500 border border-zinc-500/20">
      PENDING
    </span>
  );
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
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-6 shadow-sm space-y-6">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-4">
        <div>
          <h2 className="text-base font-bold text-zinc-900 dark:text-zinc-100">Wizard de Instalación & Verificación OCAP</h2>
          <p className="text-xs text-zinc-400 mt-0.5">Asistente automatizado de comprobación de dependencias del servidor.</p>
        </div>
        <button
          type="button"
          onClick={onValidate}
          disabled={isValidating}
          aria-busy={isValidating}
          className="flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors disabled:opacity-50"
        >
          {isValidating ? (
            <Loader2 className="w-4 h-4 animate-spin" />
          ) : (
            <Play className="w-4 h-4" />
          )}
          <span>{isValidating ? "Verificando entorno..." : "Ejecutar verificación completa"}</span>
        </button>
      </div>

      <div
        role="status"
        className={`p-4 rounded-xl border text-xs flex items-center gap-3 ${
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
            Diagnóstico real de PostgreSQL, Event Bus, almacenamiento y health checks.
            Última revisión: {new Date(lastCheckedAt).toLocaleString()}.
          </p>
        </div>
      </div>

      <div className="space-y-4">
        {steps.map((s) => (
          <div
            key={s.id}
            className="p-4 rounded-xl bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 flex items-start justify-between gap-4"
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
                {s.status === "error" ? <XCircle className="w-4 h-4" /> : <CheckCircle2 className="w-4 h-4" />}
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
    </div>
  );
}
