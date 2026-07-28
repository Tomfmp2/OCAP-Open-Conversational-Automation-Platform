import React from "react";
import { Lock, ShieldCheck, RefreshCw, KeyRound } from "lucide-react";
import { VaultStatus } from "../api/useSecurityData";

interface VaultSecurityStatusProps {
  vault: VaultStatus;
}

export function VaultSecurityStatus({ vault }: VaultSecurityStatusProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-3">
        <div className="flex items-center gap-2">
          <Lock className="w-4 h-4 text-amber-500" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Credential Vault (AES-256)</h2>
        </div>
        <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-500 bg-emerald-500/10 px-2.5 py-0.5 rounded-full border border-emerald-500/20">
          <ShieldCheck className="w-3.5 h-3.5" /> Protegido
        </span>
      </div>

      <div className="grid grid-cols-3 gap-3 text-xs">
        <div className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800">
          <span className="text-zinc-400 text-[10px]">Algoritmo de Cifrado</span>
          <p className="font-semibold text-zinc-900 dark:text-zinc-100">{vault.algorithm}</p>
        </div>

        <div className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800">
          <span className="text-zinc-400 text-[10px]">Rotación de Claves</span>
          <p className="font-semibold text-zinc-900 dark:text-zinc-100">Cada {vault.keyRotationDays} días</p>
        </div>

        <div className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800">
          <span className="text-zinc-400 text-[10px]">Secretos Cifrados</span>
          <p className="font-semibold text-amber-500">{vault.totalSecretsEncrypted} Secretos</p>
        </div>
      </div>
    </div>
  );
}
