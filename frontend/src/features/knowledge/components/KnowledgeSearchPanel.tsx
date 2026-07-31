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
 <div className="bg-white border border-neutral-200 rounded-xl p-5 shadow-sm space-y-4">
 <div className="flex items-center gap-2">
 <Search className="w-4 h-4 text-neutral-700" />
 <h2 className="text-sm font-semibold text-neutral-950">Búsqueda Semántica</h2>
 </div>

 <form onSubmit={(e) => void handleSubmit(e)} className="space-y-3">
 {error && (
 <div className="p-3 bg-white border-2 border-neutral-950 rounded-md flex items-center gap-2 text-xs text-neutral-950">
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
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs"
 />

 <div className="grid grid-cols-2 gap-3">
 <select
 value={strategy}
 onChange={(e) => setStrategy(e.target.value)}
 className="bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs"
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
 className="bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs"
 placeholder="Top K"
 />
 </div>

 <button
 type="submit"
 disabled={isSearching}
 className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-lg bg-neutral-950 hover:bg-neutral-900 text-white text-xs font-semibold disabled:opacity-50"
 >
 {isSearching && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
 Buscar
 </button>
 </form>

 {results.length > 0 && (
 <div className="space-y-2 pt-2 border-t border-neutral-100">
 {results.map((r, i) => (
 <div
 key={`${r.documentId}-${i}`}
 className="p-3 rounded-lg bg-neutral-50 border border-neutral-200 text-xs"
 >
 <div className="flex items-center justify-between mb-1">
 <span className="font-mono text-[10px] text-neutral-500">{r.documentId}</span>
 <span className="text-[10px] font-semibold text-neutral-700">Score: {r.score?.toFixed(3)}</span>
 </div>
 <p className="text-neutral-700 line-clamp-3">{r.content}</p>
 </div>
 ))}
 </div>
 )}
 </div>
 );
}
