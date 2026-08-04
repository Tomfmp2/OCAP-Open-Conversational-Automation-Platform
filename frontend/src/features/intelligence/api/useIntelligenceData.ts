import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import { getTenantId } from "@/shared/api/tokenStorage";

export interface TenantProviderConfig {
  id: string;
  tenantId: string;
  providerName: string;
  displayName: string;
  modelName: string;
  isEnabled: boolean;
  vaultSecretReference: string;
  settingsJson: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  /** Runtime: seleccionado como activo en el selector. */
  isRuntimeActive: boolean;
  healthStatus: "Healthy" | "Unhealthy" | "Unknown";
  lastPingMs: number;
  baseUrl: string | null;
  hasVaultKey: boolean;
}

export type AiProviderConfigDto = TenantProviderConfig;
export type AiProviderConfig = TenantProviderConfig;

export interface AiRuntimeStatus {
  activeProvider: string;
  activeModel: string;
  status: string;
  lastCheckedUtc: string;
}

interface ProviderHealth {
  providerName?: string;
  isHealthy?: boolean;
  latencyMs?: number;
  statusMessage?: string;
}

interface RawConfig {
  id: string;
  tenantId: string;
  providerName: string;
  displayName: string;
  modelName: string;
  isEnabled: boolean;
  vaultSecretReference: string;
  settingsJson: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

function requireTenantId(): string {
  const tenantId = getTenantId();
  if (!tenantId) throw new Error("Sesión sin tenant. Vuelve a iniciar sesión.");
  return tenantId;
}

function parseBaseUrl(settingsJson: string | null | undefined): string | null {
  if (!settingsJson?.trim()) return null;
  try {
    const parsed = JSON.parse(settingsJson) as Record<string, unknown>;
    const url = parsed.BaseUrl ?? parsed.baseUrl;
    return typeof url === "string" && url.trim() ? url.trim() : null;
  } catch {
    return null;
  }
}

export const PROVIDER_MODEL_PRESETS: Record<string, string[]> = {
  Gemini: ["gemini-3.5-flash", "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-flash"],
  OpenAI: ["gpt-4o", "gpt-4o-mini", "gpt-4.1", "o4-mini"],
  Claude: ["claude-3-5-sonnet-latest", "claude-3-5-haiku-latest", "claude-sonnet-4-20250514"],
  Ollama: ["llama3", "llama3.2", "mistral", "qwen2.5"],
};

export function useIntelligenceData() {
  const queryClient = useQueryClient();

  const query = useQuery<{
    providers: TenantProviderConfig[];
    runtime: AiRuntimeStatus | null;
  }>({
    queryKey: ["intelligenceProviders"],
    queryFn: async () => {
      const tenantId = requireTenantId();
      const [configs, healthList, runtime, catalog] = await Promise.all([
        apiClient.get<RawConfig[]>(`/api/aiproviderconfigurations/tenant/${tenantId}`),
        apiClient.get<ProviderHealth[]>("/api/providers/status").catch(() => []),
        apiClient.get<AiRuntimeStatus>("/api/ai/status").catch(() => null),
        apiClient
          .get<Array<{ name: string; defaultModel: string; isActive: boolean; priority: number }>>(
            "/api/providers"
          )
          .catch(() => []),
      ]);

      const activeName = runtime?.activeProvider ?? "";

      let providers = configs.map((c) => {
        const health = healthList.find(
          (h) => h.providerName?.toLowerCase() === c.providerName?.toLowerCase()
        );
        return {
          ...c,
          isRuntimeActive:
            !!activeName &&
            activeName.toLowerCase() === c.providerName.toLowerCase(),
          healthStatus:
            typeof health?.isHealthy === "boolean"
              ? health.isHealthy
                ? ("Healthy" as const)
                : ("Unhealthy" as const)
              : ("Unknown" as const),
          lastPingMs: Math.round(health?.latencyMs || 0),
          baseUrl: parseBaseUrl(c.settingsJson),
          hasVaultKey: Boolean(c.vaultSecretReference?.trim()),
        };
      });

      // Si aún no hay filas en DB (solo registry/.env), mostrar catálogo para poder registrar.
      if (providers.length === 0 && catalog.length > 0) {
        providers = catalog.map((p, i) => {
          const health = healthList.find(
            (h) => h.providerName?.toLowerCase() === p.name?.toLowerCase()
          );
          return {
            id: `catalog-${p.name.toLowerCase()}`,
            tenantId,
            providerName: p.name,
            displayName: `${p.name} (sin config tenant)`,
            modelName: p.defaultModel || "default",
            isEnabled: false,
            vaultSecretReference: "",
            settingsJson: "{}",
            createdAtUtc: new Date(0).toISOString(),
            updatedAtUtc: new Date(0).toISOString(),
            isRuntimeActive: Boolean(p.isActive),
            healthStatus:
              typeof health?.isHealthy === "boolean"
                ? health.isHealthy
                  ? ("Healthy" as const)
                  : ("Unhealthy" as const)
                : ("Unknown" as const),
            lastPingMs: Math.round(health?.latencyMs || 0),
            baseUrl: null,
            hasVaultKey: false,
          };
        });
      }

      return { providers, runtime };
    },
    staleTime: 10_000,
    retry: 2,
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["intelligenceProviders"] });
    void queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
  };

  const createProviderMutation = useMutation({
    mutationFn: async (payload: {
      providerName: string;
      displayName: string;
      modelName: string;
      apiKey: string;
      baseUrl?: string | null;
    }) => {
      const tenantId = requireTenantId();
      return apiClient.post("/api/aiproviderconfigurations", {
        tenantId,
        providerName: payload.providerName,
        displayName: payload.displayName,
        modelName: payload.modelName,
        apiKey: payload.apiKey,
        baseUrl: payload.baseUrl || null,
      });
    },
    onSuccess: invalidate,
  });

  const updateProviderMutation = useMutation({
    mutationFn: async (payload: {
      id: string;
      modelName: string;
      apiKey?: string | null;
      baseUrl?: string | null;
    }) => {
      const tenantId = requireTenantId();
      return apiClient.put(
        `/api/aiproviderconfigurations/tenant/${tenantId}/${payload.id}`,
        {
          modelName: payload.modelName,
          apiKey: payload.apiKey?.trim() || null,
          baseUrl: payload.baseUrl?.trim() || null,
        }
      );
    },
    onSuccess: invalidate,
  });

  const setStatusMutation = useMutation({
    mutationFn: async (payload: { id: string; enable: boolean }) => {
      const tenantId = requireTenantId();
      return apiClient.patch(
        `/api/aiproviderconfigurations/tenant/${tenantId}/${payload.id}/status?enable=${payload.enable}`,
        {}
      );
    },
    onSuccess: invalidate,
  });

  const deleteProviderMutation = useMutation({
    mutationFn: async (id: string) => {
      const tenantId = requireTenantId();
      return apiClient.delete(
        `/api/aiproviderconfigurations/tenant/${tenantId}/${id}`
      );
    },
    onSuccess: invalidate,
  });

  const selectProviderMutation = useMutation({
    mutationFn: async (providerName: string) => {
      return apiClient.post("/api/providers/select", { providerName });
    },
    onSuccess: invalidate,
  });

  const testProviderMutation = useMutation({
    mutationFn: async (providerName: string) => {
      return apiClient.post<{
        providerUsed: string;
        modelUsed: string;
        generatedText: string;
        tokensUsed: number;
        latencyMs: number;
        estimatedCostUsd: number;
      }>(
        "/api/providers/test",
        {
          providerName,
          prompt: "Responde solo: OK",
        },
        { timeout: 90_000 }
      );
    },
    onSuccess: invalidate,
  });

  /** Corrige modelos Gemini obsoletos (1.5 / 2.0) al guardar. */
  const migrateObsoleteModelMutation = useMutation({
    mutationFn: async (provider: TenantProviderConfig) => {
      if (!/1\.5|gemini-2\.0-flash/i.test(provider.modelName)) return null;
      return apiClient.put(
        `/api/aiproviderconfigurations/tenant/${requireTenantId()}/${provider.id}`,
        {
          modelName: "gemini-3.5-flash",
          apiKey: null,
          baseUrl: provider.baseUrl,
        }
      );
    },
    onSuccess: invalidate,
  });

  const modelsQuery = useQuery({
    queryKey: ["intelligenceProviderModels"],
    queryFn: () =>
      apiClient.get<Record<string, string[]>>("/api/providers/models").catch(
        () => ({}) as Record<string, string[]>
      ),
    staleTime: 60_000,
  });

  return {
    ...query,
    providers: query.data?.providers ?? [],
    runtime: query.data?.runtime ?? null,
    modelsByProvider: modelsQuery.data ?? {},
    createProviderMutation,
    updateProviderMutation,
    setStatusMutation,
    deleteProviderMutation,
    selectProviderMutation,
    testProviderMutation,
    migrateObsoleteModelMutation,
  };
}
