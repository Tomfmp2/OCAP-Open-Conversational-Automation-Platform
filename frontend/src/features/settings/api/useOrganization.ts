import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface Organization {
  name: string;
}

const key = ["organization"];

export function useOrganization() {
  return useQuery<Organization>({ queryKey: key, queryFn: () => apiClient.get<Organization>("/api/settings") });
}

export function useRenameOrganization() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => apiClient.put<Organization>("/api/settings", { name }),
    onSuccess: (organization) => queryClient.setQueryData(key, organization),
  });
}
