"use client";

import React from "react";
import { Search, Loader2, AlertCircle } from "lucide-react";
import type { KnowledgeSearchResult } from "../api/useKnowledgeData";

interface KnowledgeSearchPanelProps {
  onSearch: (query: string, strategy: string, topK: number) => Promise<KnowledgeSearchResult[]>;
  isSearching: boolean;
  error?: string | null;
}

export function KnowledgeSearchPanel({ onSearch, isSearching, error }: KnowledgeSearchPanelProps) {
  const [query, setQuery] = React.useState("");
  const [strategy, setStrategy] = React.useState("Hybrid");
  const [topK, setTopK] = React.useState(5);
  const [results, setResults] = React.useState<KnowledgeSearchResult[]>([]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!query.trim()) return;
    const data = await onSearch(query.trim(), strategy, topK);
    setResults(Array.isArray(data) ? data : []);
  };

  return (
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center gap-2">
        <Search className="w-4 h-4 text-blue-500" />
        <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Búsqueda Semántica</h2>
      </div>

      <form onSubmit={(e) => void handleSubmit(e)} className="space-y-3">
        {error && (
          <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-lg flex items-center gap-2 text-xs text-red-500">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <input
          type="text"
          required
          placeholder="Consulta de búsqueda..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          className="w-full bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-xs"
        />

        <div className="grid grid-cols-2 gap-3">
          <select
            value={strategy}
            onChange={(e) => setStrategy(e.target.value)}
            className="bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-xs"
          >
            <option value="Hybrid">Hybrid</option>
            <option value="Semantic">Semantic</option>
            <option value="Keyword">Keyword</option>
          </select>
          <input
            type="number"
            min={1}
            max={20}
            value={topK}
            onChange={(e) => setTopK(parseInt(e.target.value, 10) || 5)}
            className="bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-xs"
            placeholder="Top K"
          />
        </div>

        <button
          type="submit"
          disabled={isSearching}
          className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold disabled:opacity-50"
        >
          {isSearching && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
          Buscar
        </button>
      </form>

      {results.length > 0 && (
        <div className="space-y-2 pt-2 border-t border-zinc-100 dark:border-zinc-800">
          {results.map((r, i) => (
            <div
              key={`${r.documentId}-${i}`}
              className="p-3 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 border border-zinc-200 dark:border-zinc-800 text-xs"
            >
              <div className="flex items-center justify-between mb-1">
                <span className="font-mono text-[10px] text-zinc-400">{r.documentId}</span>
                <span className="text-[10px] font-semibold text-blue-500">Score: {r.score?.toFixed(3)}</span>
              </div>
              <p className="text-zinc-700 dark:text-zinc-300 line-clamp-3">{r.content}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
