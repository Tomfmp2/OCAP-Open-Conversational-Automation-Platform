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
    <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <BookOpen className="w-4 h-4 text-blue-500" />
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
            Bases de Conocimiento
          </h2>
        </div>
        <button
          type="button"
          onClick={onCreateClick}
          className="text-xs font-semibold text-blue-500 hover:text-blue-400"
        >
          + Nueva KB
        </button>
      </div>

      {bases.length === 0 ? (
        <p className="text-xs text-zinc-500 text-center py-6">No hay bases de conocimiento registradas.</p>
      ) : (
        <div className="space-y-2">
          {bases.map((kb) => (
            <button
              key={kb.id}
              type="button"
              onClick={() => onSelect(kb)}
              className={`w-full text-left p-3 rounded-lg border transition-all ${
                selectedId === kb.id
                  ? "border-blue-500 bg-blue-50/50 dark:bg-blue-950/30"
                  : "border-zinc-200 dark:border-zinc-800 hover:border-zinc-300 dark:hover:border-zinc-700"
              }`}
            >
              <p className="text-xs font-semibold text-zinc-900 dark:text-zinc-100">{kb.name}</p>
              <p className="text-[10px] text-zinc-400 mt-0.5 line-clamp-1">{kb.description || "—"}</p>
              <div className="flex items-center gap-3 mt-2 text-[10px] text-zinc-500">
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
