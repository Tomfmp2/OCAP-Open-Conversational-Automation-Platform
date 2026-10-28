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
    search: useMutation({
      mutationFn: (query: string) => apiClient.get<KnowledgeSearchResult[]>(`/api/knowledge/search?query=${encodeURIComponent(query)}&topK=3`),
    }),
  };
}
