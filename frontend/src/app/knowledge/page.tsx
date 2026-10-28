"use client";

import React from "react";
import { Trash2, Upload } from "lucide-react";
import {
  useKnowledgeActions,
  useKnowledgeBases,
  useKnowledgeDocuments,
  type KnowledgeDocument,
} from "@/features/knowledge/api/useKnowledgeData";
import { Badge, Button, EmptyState, ErrorState, FormError, Input, ListSkeleton, Modal, Page, PageHeader, Select } from "@/shared/components/ui";

const ACCEPTED = ".pdf,.docx,.md,.markdown,.csv,.json,.html,.htm,.xml,.txt";

export default function KnowledgePage() {
  const bases = useKnowledgeBases();
  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const [creating, setCreating] = React.useState(false);
  const base = bases.data?.find((b) => b.id === selectedId) ?? bases.data?.[0] ?? null;

  return (
    <Page>
      <PageHeader
        title="Conocimiento"
        description="Documentos con los que tus agentes responden: precios, horarios, políticas, preguntas frecuentes."
        actions={
          bases.data?.length ? (
            <Button variant="secondary" onClick={() => setCreating(true)}>
              Nueva colección
            </Button>
          ) : undefined
        }
      />

      {bases.isLoading ? (
        <ListSkeleton label="Cargando conocimiento" />
      ) : bases.isError ? (
        <ErrorState message={bases.error instanceof Error ? bases.error.message : undefined} onRetry={() => void bases.refetch()} />
      ) : !base ? (
        <EmptyState
          title="Todavía no hay documentos"
          description="Crea una colección y sube tus documentos. Los agentes los consultan al responder."
          action={<Button onClick={() => setCreating(true)}>Crear colección</Button>}
        />
      ) : (
        <>
          {bases.data!.length > 1 && (
            <div className="max-w-xs">
              <Select label="Colección" value={base.id} onChange={(e) => setSelectedId(e.target.value)}>
                {bases.data!.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </Select>
            </div>
          )}
          <Documents key={base.id} baseId={base.id} />
          <SearchTry />
        </>
      )}

      <CreateCollection
        open={creating}
        onClose={() => setCreating(false)}
        onCreated={(id) => {
          setSelectedId(id);
          setCreating(false);
        }}
      />
    </Page>
  );
}

const STATUS: Partial<Record<KnowledgeDocument["status"], { label: string; tone: "warning" | "danger" }>> = {
  Pending: { label: "Procesando", tone: "warning" },
  Processing: { label: "Procesando", tone: "warning" },
  Failed: { label: "No se pudo leer", tone: "danger" },
};

function Documents({ baseId }: { baseId: string }) {
  const documents = useKnowledgeDocuments(baseId);
  const { upload, remove, reindex } = useKnowledgeActions(baseId);
  const fileRef = React.useRef<HTMLInputElement>(null);
  const [deleting, setDeleting] = React.useState<KnowledgeDocument | null>(null);

  const uploadFiles = async (files: FileList | null) => {
    for (const file of Array.from(files ?? [])) {
      await upload.mutateAsync(file).catch(() => undefined);
    }
    if (fileRef.current) fileRef.current.value = "";
  };

  return (
    <section className="space-y-3" aria-labelledby="documents-title">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 id="documents-title" className="text-sm font-semibold text-neutral-950">
          Documentos
        </h2>
        <div className="flex gap-2">
          {!!documents.data?.length && (
            <Button
              variant="ghost"
              size="sm"
              loading={reindex.isPending}
              onClick={() => reindex.mutate()}
              title="Úsalo si cambiaste el modelo de IA: vuelve a leer todos los documentos."
            >
              Volver a procesar
            </Button>
          )}
          <Button size="sm" loading={upload.isPending} onClick={() => fileRef.current?.click()}>
            <Upload className="h-3.5 w-3.5" aria-hidden /> Subir documentos
          </Button>
          <input ref={fileRef} type="file" multiple accept={ACCEPTED} className="sr-only" tabIndex={-1} aria-hidden onChange={(e) => void uploadFiles(e.target.files)} />
        </div>
      </div>
      <FormError error={upload.error ?? reindex.error} />

      {documents.isLoading ? (
        <ListSkeleton label="Cargando documentos" rows={2} />
      ) : documents.isError ? (
        <ErrorState onRetry={() => void documents.refetch()} />
      ) : !documents.data?.length ? (
        <EmptyState title="Sin documentos" description="PDF, Word, Markdown, CSV, JSON, HTML o texto, de hasta 10 MB." />
      ) : (
        <ul className="divide-y divide-neutral-200 rounded-lg border border-neutral-200 bg-white">
          {documents.data.map((doc) => (
            <li key={doc.id} className="flex items-center gap-3 px-4 py-3 sm:px-5">
              <div className="min-w-0 flex-1">
                <p className="flex flex-wrap items-center gap-2">
                  <span className="truncate text-sm font-medium text-neutral-950">{doc.title}</span>
                  {STATUS[doc.status] && <Badge tone={STATUS[doc.status]!.tone}>{STATUS[doc.status]!.label}</Badge>}
                </p>
                <p className="text-xs text-neutral-600">
                  {doc.fileType.toUpperCase()}, subido el {new Date(doc.createdAtUtc).toLocaleDateString()}
                </p>
              </div>
              <button
                type="button"
                onClick={() => setDeleting(doc)}
                className="focus-ring rounded-md p-2 text-neutral-600 hover:bg-neutral-100 hover:text-danger"
                aria-label={`Eliminar ${doc.title}`}
              >
                <Trash2 className="h-4 w-4" aria-hidden />
              </button>
            </li>
          ))}
        </ul>
      )}

      <Modal open={deleting !== null} onClose={() => setDeleting(null)} title={`¿Eliminar ${deleting?.title ?? ""}?`}>
        <div className="space-y-5">
          <p className="text-sm text-neutral-700">Los agentes dejan de usar su contenido para responder.</p>
          <FormError error={remove.error} />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={() => setDeleting(null)}>
              Cancelar
            </Button>
            <Button variant="danger" loading={remove.isPending} onClick={() => deleting && remove.mutate(deleting.id, { onSuccess: () => setDeleting(null) })}>
              Eliminar documento
            </Button>
          </div>
        </div>
      </Modal>
    </section>
  );
}

/** Lo que encontraría un agente con esa pregunta: sirve para comprobar que el documento se entiende. */
function SearchTry() {
  const { search } = useKnowledgeActions(null);
  const [query, setQuery] = React.useState("");

  return (
    <section className="space-y-3 border-t border-neutral-200 pt-6" aria-labelledby="search-title">
      <div>
        <h2 id="search-title" className="text-sm font-semibold text-neutral-950">
          Probar
        </h2>
        <p className="text-sm text-neutral-600">Pregunta como lo haría un cliente y mira qué encuentra el agente.</p>
      </div>
      <form
        className="flex max-w-xl items-end gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          if (query.trim()) search.mutate(query.trim());
        }}
      >
        <div className="min-w-0 flex-1">
          <Input label="Pregunta" placeholder="¿A qué hora abren los sábados?" value={query} onChange={(e) => setQuery(e.target.value)} />
        </div>
        <Button type="submit" variant="secondary" loading={search.isPending} disabled={!query.trim()}>
          Buscar
        </Button>
      </form>
      <FormError error={search.error} />
      {search.data && (
        <div aria-live="polite">
          {search.data.length === 0 ? (
            <p className="text-sm text-neutral-600">No encontró nada relacionado. Sube un documento que responda esa pregunta.</p>
          ) : (
            <ul className="space-y-2">
              {search.data.map((result, index) => (
                <li key={index} className="rounded-lg border border-neutral-200 bg-white p-4">
                  <p className="text-xs font-medium text-neutral-600">{result.documentTitle}</p>
                  <p className="mt-1 line-clamp-4 text-sm text-neutral-800">{result.content}</p>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </section>
  );
}

function CreateCollection({ open, onClose, onCreated }: { open: boolean; onClose: () => void; onCreated: (id: string) => void }) {
  const { createBase } = useKnowledgeActions(null);
  const [name, setName] = React.useState("");

  return (
    <Modal open={open} onClose={onClose} title="Nueva colección" description="Agrupa documentos de un mismo tema.">
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          createBase.mutate(name.trim(), {
            onSuccess: (created) => {
              setName("");
              onCreated(created.id);
            },
          });
        }}
      >
        <Input label="Nombre" required maxLength={100} placeholder="Atención al cliente" value={name} onChange={(e) => setName(e.target.value)} />
        <FormError error={createBase.error} />
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" loading={createBase.isPending}>
            Crear colección
          </Button>
        </div>
      </form>
    </Modal>
  );
}
