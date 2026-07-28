"use client";

import React from "react";
import { Store, Download, Star, CheckCircle2, ArrowUpRight } from "lucide-react";
import { MarketplaceItem } from "../api/useMarketplaceData";

interface MarketplaceItemCardProps {
  item: MarketplaceItem;
  onInstallToggle: (id: string) => void;
}

export function MarketplaceItemCard({ item, onInstallToggle }: MarketplaceItemCardProps) {
  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4 hover:border-zinc-300 dark:hover:border-zinc-700 transition-all">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-blue-500/10 text-blue-500 flex items-center justify-center font-bold text-sm border border-blue-500/20">
            <Store className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{item.name}</h3>
              <span className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-zinc-200 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400">
                {item.version}
              </span>
            </div>
            <p className="text-xs text-zinc-400 font-mono">Por: {item.author}</p>
          </div>
        </div>

        <span className="text-[10px] px-2 py-0.5 rounded-full bg-blue-500/10 text-blue-500 font-mono uppercase font-bold">
          {item.category}
        </span>
      </div>

      <p className="text-xs text-zinc-500 leading-relaxed">{item.description}</p>

      <div className="flex items-center justify-between pt-2 border-t border-zinc-100 dark:border-zinc-800 text-xs">
        <div className="flex items-center gap-3 text-zinc-400">
          <span className="flex items-center gap-1">
            <Download className="w-3.5 h-3.5" />
            {item.downloads.toLocaleString()}
          </span>
          <span className="flex items-center gap-1 text-amber-500">
            <Star className="w-3.5 h-3.5 fill-amber-500" />
            {item.rating}
          </span>
        </div>

        <button
          onClick={() => onInstallToggle(item.id)}
          className={`flex items-center gap-1.5 px-3 py-1 rounded-lg text-xs font-semibold shadow-sm transition-colors ${
            item.installed
              ? "bg-zinc-100 dark:bg-zinc-800 text-zinc-700 dark:text-zinc-300 hover:bg-zinc-200"
              : "bg-blue-600 hover:bg-blue-500 text-white"
          }`}
        >
          {item.installed ? (
            <>
              <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />
              <span>Instalado</span>
            </>
          ) : (
            <>
              <Download className="w-3.5 h-3.5" />
              <span>Instalar</span>
            </>
          )}
        </button>
      </div>
    </div>
  );
}
