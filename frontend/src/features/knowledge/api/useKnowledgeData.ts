import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface KnowledgeBase {
  id: string;
  name: string;
  documentCount: number;
}

export type DocumentStatus = "Pending" | "Processing" | "Indexed" | "Failed" | "Archived";

export interface KnowledgeDocument {
  id: string;
  title: string;
  fileType: string;
  status: DocumentStatus;
  createdAtUtc: string;
}

export interface KnowledgeSearchResult {
  documentTitle: string;
  content: string;
}

const BASES_KEY = ["knowledge", "bases"];
const documentsKey = (baseId: string) => ["knowledge", "documents", baseId];

export function useKnowledgeBases() {
  return useQuery<KnowledgeBase[]>({ queryKey: BASES_KEY, queryFn: () => apiClient.get<KnowledgeBase[]>("/api/knowledge") });
}

export function useKnowledgeDocuments(baseId: string | null) {
  return useQuery<KnowledgeDocument[]>({
    queryKey: documentsKey(baseId ?? ""),
    queryFn: () => apiClient.get<KnowledgeDocument[]>(`/api/knowledge/${baseId}/documents`),
    enabled: baseId !== null,
    // Mientras un documento se procesa, la lista se consulta de nuevo para mostrar cuándo queda listo.
    refetchInterval: (query) => (query.state.data?.some((d) => d.status === "Pending" || d.status === "Processing") ? 3000 : false),
  });
}

export function useKnowledgeActions(baseId: string | null) {
  const queryClient = useQueryClient();
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: BASES_KEY });
    if (baseId) void queryClient.invalidateQueries({ queryKey: documentsKey(baseId) });
  };

  return {
    createBase: useMutation({
      mutationFn: (name: string) => apiClient.post<KnowledgeBase>("/api/knowledge", { name, description: "" }),
      onSuccess: () => void queryClient.invalidateQueries({ queryKey: BASES_KEY }),
    }),
    upload: useMutation({
      mutationFn: (file: File) => {
        const form = new FormData();
        form.append("file", file);
        form.append("knowledgeBaseId", baseId ?? "");
        form.append("category", "General");
        return apiClient.upload("/api/knowledge/upload", form);
      },
      onSettled: refresh,
    }),
    remove: useMutation({
      mutationFn: (documentId: string) => apiClient.delete(`/api/knowledge/${documentId}`),
      onSuccess: refresh,
    }),
    reindex: useMutation({
      mutationFn: () => apiClient.post("/api/knowledge/reindex", { knowledgeBaseId: baseId }),
      onSuccess: refresh,
    }),
    /** Pregunta frecuente o nota escrita en el panel; se indexa como un documento. */
    addText: useMutation({
      mutationFn: (m: { title: string; content: string }) => apiClient.post("/api/knowledge/text", { knowledgeBaseId: baseId, ...m }),
      onSuccess: refresh,
    }),
    importUrl: useMutation({
      mutationFn: (url: string) => apiClient.post("/api/knowledge/url", { knowledgeBaseId: baseId, url }, { timeout: 60_000, skipRetry: true }),
      onSuccess: refresh,
    }),
    search: useMutation({
      mutationFn: (query: string) => apiClient.get<KnowledgeSearchResult[]>(`/api/knowledge/search?query=${encodeURIComponent(query)}&topK=3`),
    }),
  };
}

/** Lo que OCAP leyó del documento, en fragmentos: es lo que usan los agentes. */
export function useDocumentContent(documentId: string | null) {
  return useQuery<{ chunks: string[] }>({
    queryKey: ["knowledge", "content", documentId],
    queryFn: () => apiClient.get(`/api/knowledge/documents/${documentId}/content`),
    enabled: documentId !== null,
  });
}
