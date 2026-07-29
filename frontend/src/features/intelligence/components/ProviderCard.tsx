import React from "react";
import { Cpu, Lock, CheckCircle2, RefreshCw } from "lucide-react";
import { AiProviderConfig } from "../api/useIntelligenceData";

interface ProviderCardProps {
  provider: AiProviderConfig;
  onTest: (id: string) => void;
  isTesting: boolean;
}

export function ProviderCard({ provider, onTest, isTesting }: ProviderCardProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4 hover:border-zinc-300 dark:hover:border-zinc-700 transition-all">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-500/10 text-blue-500 flex items-center justify-center font-bold text-sm border border-blue-500/20">
            <Cpu className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{provider.displayName}</h3>
              {provider.isEncrypted && (
                <span className="inline-flex items-center gap-0.5 text-[9px] font-mono px-1.5 py-0.2 rounded bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20">
                  <Lock className="w-2.5 h-2.5" /> AES-256
                </span>
              )}
            </div>
            <p className="text-xs text-zinc-400 font-mono">Modelo por defecto: {provider.defaultModel}</p>
          </div>
        </div>

        {provider.isActive ? (
          <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 px-2.5 py-0.5 rounded-full border border-emerald-500/20">
            <CheckCircle2 className="w-3.5 h-3.5" /> Activo (Prioridad #{provider.priorityOrder})
          </span>
        ) : (
          <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-zinc-400 bg-zinc-100 dark:bg-zinc-800 px-2.5 py-0.5 rounded-full">
            Inactivo
          </span>
        )}
      </div>

      <div className="grid grid-cols-3 gap-2 text-xs pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <div>
          <span className="text-zinc-400 text-[10px]">Tokens Procesados</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">{(provider.totalTokensProcessed / 1000000).toFixed(2)}M</p>
        </div>
        <div>
          <span className="text-zinc-400 text-[10px]">Gasto Acumulado</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">${provider.monthlyCostUsd.toFixed(2)} USD</p>
        </div>
        <div>
          <span className="text-zinc-400 text-[10px]">Latencia Ping</span>
          <p className="font-semibold text-zinc-800 dark:text-zinc-200">{provider.lastPingMs > 0 ? `${provider.lastPingMs} ms` : "Offline"}</p>
        </div>
      </div>

      <div className="flex items-center justify-between pt-2 border-t border-zinc-100 dark:border-zinc-800">
        <span className="text-[11px] text-zinc-400 font-mono">Vault Status: Protected</span>
        <button
          onClick={() => onTest(provider.id)}
          disabled={isTesting}
          className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-zinc-50 dark:bg-zinc-800/60 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-700 transition-colors disabled:opacity-40"
        >
          <RefreshCw className={`w-3 h-3 ${isTesting ? "animate-spin" : ""}`} />
          <span>Probar Latencia & API Key</span>
        </button>
      </div>
    </div>
  );
}
