import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import { getTenantId } from "@/shared/api/tokenStorage";

export type ProviderName = "Gemini" | "OpenAI" | "Claude" | "Ollama" | "OpenAICompatible";

export const PROVIDER_LABELS: Record<ProviderName, string> = {
  Gemini: "Gemini",
  OpenAI: "OpenAI",
  Claude: "Claude",
  Ollama: "Ollama",
  OpenAICompatible: "Compatible con OpenAI",
};

export const PROVIDER_MODEL_PRESETS: Record<ProviderName, string[]> = {
  Gemini: ["gemini-3.5-flash", "gemini-2.5-flash"],
  OpenAI: ["gpt-4o", "gpt-4o-mini", "gpt-4.1", "o4-mini"],
  Claude: ["claude-sonnet-5-5", "claude-haiku-5-5", "claude-opus-5-5"],
  Ollama: ["llama3", "llama3.2", "mistral", "qwen2.5"],
  OpenAICompatible: [],
};

/** Servicios con API al estilo de OpenAI: solo cambia la dirección. */
export const OPENAI_COMPATIBLE_SERVICES = [
  { name: "NVIDIA", baseUrl: "https://integrate.api.nvidia.com/v1" },
  { name: "Groq", baseUrl: "https://api.groq.com/openai/v1" },
  { name: "OpenRouter", baseUrl: "https://openrouter.ai/api/v1" },
  { name: "DeepSeek", baseUrl: "https://api.deepseek.com/v1" },
  { name: "Mistral", baseUrl: "https://api.mistral.ai/v1" },
] as const;

/** Proveedores que llaman a una dirección elegida por el usuario. */
export const usesBaseUrl = (provider: ProviderName) => provider === "Ollama" || provider === "OpenAICompatible";

export const OLLAMA_DEFAULT_URL = "http://host.docker.internal:11434";

export interface AiModel {
  id: string;
  providerName: ProviderName;
  displayName: string;
  modelName: string;
  isEnabled: boolean;
  baseUrl: string | null;
  /** El que usa la organización cuando un canal no elige otro. */
  isActive: boolean;
}

interface RawConfig {
  id: string;
  providerName: ProviderName;
  displayName: string;
  modelName: string;
  isEnabled: boolean;
  settingsJson?: string;
}

const KEY = ["aiModels"];

function tenantPath() {
  const tenantId = getTenantId();
  if (!tenantId) throw new Error("La sesión no tiene organización. Vuelve a iniciar sesión.");
  return `/api/aiproviderconfigurations/tenant/${tenantId}`;
}

function baseUrlOf(settingsJson?: string): string | null {
  try {
    const settings = JSON.parse(settingsJson || "{}") as { BaseUrl?: string; baseUrl?: string };
    return settings.BaseUrl ?? settings.baseUrl ?? null;
  } catch {
    return null;
  }
}

export function useAiModels() {
  return useQuery<AiModel[]>({
    queryKey: KEY,
    queryFn: async () => {
      const [configs, providers] = await Promise.all([
        apiClient.get<RawConfig[]>(tenantPath()),
        apiClient.get<Array<{ id: string | null; isActive: boolean }>>("/api/providers").catch(() => []),
      ]);
      const active = providers.find((p) => p.isActive)?.id;
      return configs.map((c) => ({
        id: c.id,
        providerName: c.providerName,
        displayName: c.displayName || c.providerName,
        modelName: c.modelName,
        isEnabled: c.isEnabled,
        baseUrl: baseUrlOf(c.settingsJson),
        isActive: c.id === active,
      }));
    },
  });
}

export function useAiModelActions() {
  const queryClient = useQueryClient();
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: KEY });
    void queryClient.invalidateQueries({ queryKey: ["intelligenceProviders", "options"] });
  };

  return {
    create: useMutation({
      mutationFn: (m: { providerName: ProviderName; displayName: string; modelName: string; apiKey: string; baseUrl: string | null }) =>
        apiClient.post<{ id: string }>("/api/aiproviderconfigurations", { tenantId: getTenantId(), ...m }),
      onSuccess: refresh,
    }),
    update: useMutation({
      mutationFn: (m: { id: string; modelName: string; apiKey: string | null; baseUrl: string | null }) =>
        apiClient.put(`${tenantPath()}/${m.id}`, { modelName: m.modelName, apiKey: m.apiKey || null, baseUrl: m.baseUrl || null }),
      onSuccess: refresh,
    }),
    setEnabled: useMutation({
      mutationFn: (m: { id: string; enable: boolean }) => apiClient.patch(`${tenantPath()}/${m.id}/status?enable=${m.enable}`, {}),
      onSuccess: refresh,
    }),
    remove: useMutation({ mutationFn: (id: string) => apiClient.delete(`${tenantPath()}/${id}`), onSuccess: refresh }),
    /** Recibe el id de la configuración: puede haber varias del mismo proveedor. */
    setActive: useMutation({ mutationFn: (configId: string) => apiClient.post("/api/providers/select", { providerName: configId }), onSuccess: refresh }),
    test: useMutation({
      mutationFn: (configId: string) =>
        apiClient.post<{ modelUsed: string; generatedText: string; latencyMs: number }>(
          "/api/providers/test",
          { providerName: configId, prompt: "Responde solo: OK" },
          { timeout: 90_000, skipRetry: true }
        ),
    }),
  };
}

/**
 * Modelos que ofrece el proveedor. Con configurationId usa la clave guardada; si no, la escrita en el formulario,
 * que solo viaja a la API para esta consulta.
 */
export function useProviderModels() {
  return useMutation({
    mutationFn: (m: { configurationId?: string; providerName?: ProviderName; apiKey?: string; baseUrl?: string | null }) =>
      apiClient.post<string[]>("/api/aiproviderconfigurations/models", m, { timeout: 30_000, skipRetry: true }),
  });
}
