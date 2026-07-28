"use client";

import React from "react";
import { Code2, Key, Radio, BookOpen, RefreshCw } from "lucide-react";
import { useDeveloperData } from "@/features/developer/api/useDeveloperData";
import { ApiKeyManager } from "@/features/developer/components/ApiKeyManager";
import { WebhookManager } from "@/features/developer/components/WebhookManager";
import { DeveloperSkeleton } from "@/features/developer/components/DeveloperSkeleton";

export default function DeveloperPage() {
  const { data, isLoading, refetch, isFetching } = useDeveloperData();

  if (isLoading) {
    return <DeveloperSkeleton />;
  }

  const { apiKeys, webhooks } = data || { apiKeys: [], webhooks: [] };

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <Code2 className="w-5 h-5 text-blue-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Developer Center (API Keys, OAuth & Webhooks)
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Herramientas para desarrolladores e integración programática con la API de OCAP.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors shadow-sm disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            <span>Actualizar</span>
          </button>
          <button className="flex items-center gap-2 px-3.5 py-1.5 rounded-lg bg-zinc-800 hover:bg-zinc-700 text-white text-xs font-semibold shadow-md transition-colors">
            <BookOpen className="w-3.5 h-3.5" />
            <span>Documentación OpenAPI</span>
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6">
        <ApiKeyManager keys={apiKeys} />
        <WebhookManager webhooks={webhooks} />
      </div>
    </div>
  );
}
