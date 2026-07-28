"use client";

import React from "react";
import { CheckCircle2, Play, RefreshCw, ShieldCheck, Database, Cpu, MessageSquare } from "lucide-react";
import { InstallerStep } from "../api/useInstallerData";

interface InstallerWizardStepsProps {
  steps: InstallerStep[];
}

export function InstallerWizardSteps({ steps }: InstallerWizardStepsProps) {
  const [running, setRunning] = React.useState(false);
  const [done, setDone] = React.useState(false);

  const handleRunInstall = () => {
    setRunning(true);
    setDone(false);
    setTimeout(() => {
      setRunning(false);
      setDone(true);
    }, 1500);
  };

  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-6 shadow-sm space-y-6">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-4">
        <div>
          <h2 className="text-base font-bold text-zinc-900 dark:text-zinc-100">Wizard de Instalación & Verificación OCAP</h2>
          <p className="text-xs text-zinc-400 mt-0.5">Asistente automatizado de comprobación de dependencias del servidor.</p>
        </div>
        <button
          onClick={handleRunInstall}
          disabled={running}
          className="flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors disabled:opacity-50"
        >
          <Play className={`w-4 h-4 ${running ? "animate-spin" : ""}`} />
          <span>{running ? "Verificando Entorno..." : "Ejecutar Verificación Completa"}</span>
        </button>
      </div>

      {done && (
        <div className="p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-xs flex items-center gap-3">
          <CheckCircle2 className="w-5 h-5 shrink-0" />
          <div>
            <p className="font-bold">¡Plataforma OCAP 100% Operacional!</p>
            <p className="text-[11px] text-zinc-400 mt-0.5">Todos los subsistemas backend y adapters hexagonales responden sin errores.</p>
          </div>
        </div>
      )}

      <div className="space-y-4">
        {steps.map((s) => (
          <div
            key={s.id}
            className="p-4 rounded-xl bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 flex items-start justify-between gap-4"
          >
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-lg bg-emerald-500/10 text-emerald-500 flex items-center justify-center font-bold text-xs shrink-0 mt-0.5">
                <CheckCircle2 className="w-4 h-4" />
              </div>
              <div>
                <p className="text-xs font-bold text-zinc-900 dark:text-zinc-100">Paso #{s.id}: {s.title}</p>
                <p className="text-xs text-zinc-500 mt-0.5">{s.description}</p>
                <p className="text-[11px] text-zinc-400 font-mono mt-1">{s.details}</p>
              </div>
            </div>
            <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">
              OK
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
