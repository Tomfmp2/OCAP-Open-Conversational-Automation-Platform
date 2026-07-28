import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

export interface AiProviderConfigDto {
  id: string;
  providerType: string;
  displayName: string;
  defaultModel: string;
  isEncrypted: boolean;
  isActive: boolean;
  priorityOrder: number;
  totalTokensProcessed: number;
  monthlyCostUsd: number;
  healthStatus: string;
  lastPingMs: number;
}

export type AiProviderConfig = AiProviderConfigDto;

export function useIntelligenceData() {
  const queryClient = useQueryClient();

  const query = useQuery<AiProviderConfigDto[]>({
    queryKey: ["intelligenceProviders"],
    queryFn: async () => {
      const tenantId = "e8392929-1111-4444-8888-999999999999";
      const res = await fetch(`/api/aiproviderconfigurations/tenant/${tenantId}`);
      if (!res.ok) {
        return [];
      }
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    },
    staleTime: 15000,
  });

  const testProviderMutation = useMutation({
    mutationFn: async (providerId: string) => {
      const res = await fetch(`/api/aiproviderconfigurations/${providerId}/test`, { method: "POST" });
      if (!res.ok) throw new Error("Error testing provider connection");
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["intelligenceProviders"] });
    },
  });

  return { ...query, testProviderMutation };
}
