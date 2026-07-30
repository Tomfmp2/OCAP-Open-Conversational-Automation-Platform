"use client";

import React from "react";
import { BookOpen, RefreshCw, RotateCcw, Inbox } from "lucide-react";
import { useKnowledgeData } from "@/features/knowledge/api/useKnowledgeData";
import { KnowledgeBaseList } from "@/features/knowledge/components/KnowledgeBaseList";
import { CreateKnowledgeBaseModal } from "@/features/knowledge/components/CreateKnowledgeBaseModal";
import { KnowledgeUploadPanel } from "@/features/knowledge/components/KnowledgeUploadPanel";
import { KnowledgeSearchPanel } from "@/features/knowledge/components/KnowledgeSearchPanel";

export default function KnowledgePage() {
  const {
    data: bases,
    isLoading,
    refetch,
    isFetching,
    jobs,
    createMutation,
    uploadMutation,
    searchMutation,
    reindexMutation,
    refetchJobs,
  } = useKnowledgeData();

  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const [modalOpen, setModalOpen] = React.useState(false);
  const [createError, setCreateError] = React.useState<string | null>(null);

  const kbList = bases || [];
  const selectedKb = kbList.find((kb) => kb.id === selectedId) ?? kbList[0] ?? null;

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto p-12 text-center text-sm text-zinc-500">
        Cargando bases de conocimiento...
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-zinc-200 dark:border-zinc-800 pb-4">
        <div>
          <div className="flex items-center gap-2">
            <BookOpen className="w-5 h-5 text-blue-500" />
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">
              Knowledge Base & RAG Engine
            </h1>
          </div>
          <p className="text-xs text-zinc-500 mt-1">
            Gestión de documentos, indexación vectorial y búsqueda semántica multi-tenant.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => {
              void refetch();
              void refetchJobs();
            }}
            disabled={isFetching}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-xs font-medium disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            Actualizar
          </button>
          {selectedKb && (
            <button
              type="button"
              onClick={() => reindexMutation.mutate(selectedKb.id)}
              disabled={reindexMutation.isPending}
              className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-purple-600 hover:bg-purple-500 text-white text-xs font-semibold disabled:opacity-50"
            >
              <RotateCcw className={`w-3.5 h-3.5 ${reindexMutation.isPending ? "animate-spin" : ""}`} />
              Reindexar
            </button>
          )}
        </div>
      </div>

      {kbList.length === 0 ? (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-12 text-center space-y-4">
          <Inbox className="w-6 h-6 text-zinc-400 mx-auto" />
          <h3 className="text-sm font-bold text-zinc-900 dark:text-zinc-100">No hay bases de conocimiento</h3>
          <button
            type="button"
            onClick={() => setModalOpen(true)}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold"
          >
            Crear primera KB
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <KnowledgeBaseList
            bases={kbList}
            selectedId={selectedKb?.id}
            onSelect={(kb) => setSelectedId(kb.id)}
            onCreateClick={() => setModalOpen(true)}
          />
          <KnowledgeUploadPanel
            selectedKb={selectedKb}
            onUpload={async (file, category) => {
              if (!selectedKb) return;
              await uploadMutation.mutateAsync({
                file,
                knowledgeBaseId: selectedKb.id,
                category,
              });
            }}
            isUploading={uploadMutation.isPending}
            error={uploadMutation.error?.message}
          />
          <KnowledgeSearchPanel
            onSearch={async (query, strategy, topK) =>
              searchMutation.mutateAsync({ query, strategy, topK })
            }
            isSearching={searchMutation.isPending}
            error={searchMutation.error?.message}
          />
        </div>
      )}

      {jobs.length > 0 && (
        <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800/80 rounded-xl p-5 shadow-sm">
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100 mb-3">Jobs de Indexación</h2>
          <div className="space-y-2">
            {jobs.slice(0, 5).map((job) => (
              <div
                key={job.id}
                className="flex items-center justify-between p-2 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 text-xs"
              >
                <span className="font-mono text-zinc-500">{job.type || job.id}</span>
                <span className="font-semibold text-zinc-700 dark:text-zinc-300">{job.status}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      <CreateKnowledgeBaseModal
        open={modalOpen}
        onClose={() => {
          setModalOpen(false);
          setCreateError(null);
        }}
        isCreating={createMutation.isPending}
        error={createError}
        onCreate={async (payload) => {
          try {
            setCreateError(null);
            await createMutation.mutateAsync(payload);
          } catch (err) {
            setCreateError(err instanceof Error ? err.message : "Error al crear KB");
            throw err;
          }
        }}
      />
    </div>
  );
}
