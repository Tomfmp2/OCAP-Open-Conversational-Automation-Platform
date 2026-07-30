"use client";

import React from "react";
import { BookOpen, RefreshCw, RotateCcw, Plus } from "lucide-react";
import { useKnowledgeData } from "@/features/knowledge/api/useKnowledgeData";
import { KnowledgeBaseList } from "@/features/knowledge/components/KnowledgeBaseList";
import { CreateKnowledgeBaseModal } from "@/features/knowledge/components/CreateKnowledgeBaseModal";
import { KnowledgeUploadPanel } from "@/features/knowledge/components/KnowledgeUploadPanel";
import { KnowledgeSearchPanel } from "@/features/knowledge/components/KnowledgeSearchPanel";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Button } from "@/shared/components/ui/Button";
import { Surface } from "@/shared/components/ui/Surface";
import { EmptyState } from "@/shared/components/ui/EmptyState";
import { ErrorState } from "@/shared/components/ui/ErrorState";
import { Badge } from "@/shared/components/ui/Badge";

export default function KnowledgePage() {
  const {
    data: bases,
    isLoading,
    isError,
    error,
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
      <div className="mx-auto max-w-7xl p-12 text-center text-sm text-zinc-500">
        Cargando bases de conocimiento...
      </div>
    );
  }

  if (isError) {
    return <div className="mx-auto max-w-7xl"><ErrorState message={error instanceof Error ? error.message : undefined} onRetry={() => void refetch()} /></div>;
  }

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PageHeader
        title="Conocimiento"
        description="Bases documentales, indexación y búsqueda semántica."
        icon={<BookOpen className="h-5 w-5 text-blue-500" />}
        actions={<>
          <Button variant="secondary" size="sm" onClick={() => {
              void refetch();
              void refetchJobs();
            }} loading={isFetching}><RefreshCw className="h-3.5 w-3.5" /> Actualizar</Button>
          {selectedKb && (
            <Button size="sm" onClick={() => reindexMutation.mutate(selectedKb.id)} loading={reindexMutation.isPending}>
              <RotateCcw className="h-3.5 w-3.5" /> Reindexar
            </Button>
          )}
        </>}
      />

      {kbList.length === 0 ? (
        <EmptyState title="No hay bases de conocimiento" description="Crea una base para poder cargar e indexar documentos." action={<Button size="sm" onClick={() => setModalOpen(true)}><Plus className="h-4 w-4" /> Crear base</Button>} />
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
        <Surface padding="md">
          <h2 className="text-sm font-semibold text-zinc-900 dark:text-zinc-100 mb-3">Jobs de Indexación</h2>
          <div className="space-y-2">
            {jobs.slice(0, 5).map((job) => (
              <div
                key={job.id}
                className="flex items-center justify-between p-2 rounded-lg bg-zinc-50 dark:bg-zinc-950/50 text-xs"
              >
                <span className="font-mono text-zinc-500">{job.type || job.id}</span>
                <Badge tone={job.status === "Failed" ? "danger" : job.status === "Completed" ? "success" : "info"}>{job.status}</Badge>
              </div>
            ))}
          </div>
        </Surface>
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
