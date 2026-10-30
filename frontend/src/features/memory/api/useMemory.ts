import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface MemoryItem {
  id: string;
  /** Fact: dato duradero; Episode: resumen de una conversación pasada. */
  kind: "Fact" | "Episode";
  key: string | null;
  text: string;
  confirmedAt: string;
  expiresAt: string;
}

/**
 * Recuerdos de una persona. `basePath` es el recurso de la API: la memoria de un contacto
 * (/api/inbox/{id}/memory) o la del usuario del panel (/api/agents/principal/memory).
 */
export function useMemory(basePath: string, enabled: boolean) {
  return useQuery<MemoryItem[]>({ queryKey: ["memory", basePath], queryFn: () => apiClient.get(basePath), enabled });
}

export function useMemoryActions(basePath: string) {
  const queryClient = useQueryClient();
  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["memory", basePath] });
  return {
    edit: useMutation({ mutationFn: (m: { id: string; text: string }) => apiClient.put(`${basePath}/${m.id}`, { text: m.text }), onSuccess: refresh }),
    forget: useMutation({ mutationFn: (id?: string) => apiClient.delete(id ? `${basePath}/${id}` : basePath), onSuccess: refresh }),
  };
}
