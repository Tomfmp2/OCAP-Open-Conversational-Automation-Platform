"use client";

import React from "react";
import { BookOpen, FileText, Layers } from "lucide-react";
import type { KnowledgeBase } from "../api/useKnowledgeData";

interface KnowledgeBaseListProps {
 bases: KnowledgeBase[];
 selectedId?: string;
 onSelect: (kb: KnowledgeBase) => void;
 onCreateClick: () => void;
}

export function KnowledgeBaseList({
 bases,
 selectedId,
 onSelect,
 onCreateClick,
}: KnowledgeBaseListProps) {
 return (
 <div className="bg-white border border-neutral-200 rounded-xl p-5 shadow-sm space-y-4">
 <div className="flex items-center justify-between">
 <div className="flex items-center gap-2">
 <BookOpen className="w-4 h-4 text-neutral-700" />
 <h2 className="text-sm font-semibold text-neutral-950">
 Bases de Conocimiento
 </h2>
 </div>
 <button
 type="button"
 onClick={onCreateClick}
 className="text-xs font-semibold text-neutral-700 hover:text-neutral-600"
 >
 + Nueva KB
 </button>
 </div>

 {bases.length === 0 ? (
 <p className="text-xs text-neutral-500 text-center py-6">No hay bases de conocimiento registradas.</p>
 ) : (
 <div className="space-y-2">
 {bases.map((kb) => (
 <button
 key={kb.id}
 type="button"
 onClick={() => onSelect(kb)}
 className={`w-full text-left p-3 rounded-lg border transition-all ${
 selectedId === kb.id
 ? "border-neutral-950 bg-neutral-100/50"
 : "border-neutral-200 hover:border-neutral-300"
 }`}
 >
 <p className="text-xs font-semibold text-neutral-950">{kb.name}</p>
 <p className="text-[10px] text-neutral-500 mt-0.5 line-clamp-1">{kb.description || "—"}</p>
 <div className="flex items-center gap-3 mt-2 text-[10px] text-neutral-500">
 <span className="flex items-center gap-1">
 <FileText className="w-3 h-3" /> {kb.documentCount} docs
 </span>
 <span className="flex items-center gap-1">
 <Layers className="w-3 h-3" /> {kb.vectorCount} vectores
 </span>
 </div>
 </button>
 ))}
 </div>
 )}
 </div>
 );
}
