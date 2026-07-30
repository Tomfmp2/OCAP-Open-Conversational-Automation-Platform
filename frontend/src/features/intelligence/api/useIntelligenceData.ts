import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import { getTenantId } from "@/shared/api/tokenStorage";

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

interface ProviderHealth {
  providerName?: string;
  isHealthy?: boolean;
  latencyMs?: number;
}

export function useIntelligenceData() {
  const queryClient = useQueryClient();

  const query = useQuery<AiProviderConfigDto[]>({
    queryKey: ["intelligenceProviders"],
    queryFn: async () => {
      const [providersList, healthList] = await Promise.all([
        apiClient.get<
          Array<{ name: string; defaultModelName: string; isActive: boolean; priorityOrder: number }>
        >("/api/providers"),
        apiClient.get<ProviderHealth[]>("/api/providers/status").catch(() => []),
      ]);

      return providersList.map((p, i) => {
        const health = healthList.find(
          (h) => h.providerName?.toLowerCase() === p.name?.toLowerCase()
        );
        return {
          id: `provider-${p.name.toLowerCase()}`,
          providerType: p.name,
          displayName: `${p.name} Provider`,
          defaultModel: p.defaultModelName || "default",
          isEncrypted: true,
          isActive: p.isActive,
          priorityOrder: p.priorityOrder || i + 1,
          totalTokensProcessed: 0,
          monthlyCostUsd: 0,
          healthStatus: health?.isHealthy ? "Healthy" : "Operational",
          lastPingMs: Math.round(health?.latencyMs || 0),
        };
      });
    },
    staleTime: 15000,
    retry: 2,
  });

  const testProviderMutation = useMutation({
    mutationFn: async (providerName: string) => {
      return apiClient.post("/api/providers/test", {
        providerName,
        prompt: "Prueba de conectividad",
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["intelligenceProviders"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  const createProviderMutation = useMutation({
    mutationFn: async (payload: {
      providerType: string;
      displayName: string;
      modelName: string;
      apiKey: string;
    }) => {
      const tenantId = getTenantId();
      if (!tenantId) throw new Error("Tenant no disponible");

      return apiClient.post("/api/aiproviderconfigurations", {
        tenantId,
        providerName: payload.providerType,
        displayName: payload.displayName,
        modelName: payload.modelName,
        apiKey: payload.apiKey,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["intelligenceProviders"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  return { ...query, testProviderMutation, createProviderMutation };
}
