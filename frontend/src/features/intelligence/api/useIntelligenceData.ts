import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

export interface AiProviderConfig {
  id: string;
  providerType: "OpenAI" | "Gemini" | "Ollama" | "Local";
  displayName: string;
  defaultModel: string;
  isEncrypted: boolean;
  isActive: boolean;
  priorityOrder: number;
  totalTokensProcessed: number;
  monthlyCostUsd: number;
  healthStatus: "healthy" | "unhealthy" | "testing";
  lastPingMs: number;
}

const MOCK_PROVIDERS: AiProviderConfig[] = [
  {
    id: "prov-openai",
    providerType: "OpenAI",
    displayName: "OpenAI Platform",
    defaultModel: "gpt-4o",
    isEncrypted: true,
    isActive: true,
    priorityOrder: 1,
    totalTokensProcessed: 8490000,
    monthlyCostUsd: 28.4,
    healthStatus: "healthy",
    lastPingMs: 140,
  },
  {
    id: "prov-gemini",
    providerType: "Gemini",
    displayName: "Google Gemini AI",
    defaultModel: "gemini-1.5-pro",
    isEncrypted: true,
    isActive: true,
    priorityOrder: 2,
    totalTokensProcessed: 4120000,
    monthlyCostUsd: 14.45,
    healthStatus: "healthy",
    lastPingMs: 110,
  },
  {
    id: "prov-ollama",
    providerType: "Ollama",
    displayName: "Ollama Local Engine",
    defaultModel: "llama3:70b",
    isEncrypted: false,
    isActive: true,
    priorityOrder: 3,
    totalTokensProcessed: 1200000,
    monthlyCostUsd: 0,
    healthStatus: "healthy",
    lastPingMs: 18,
  },
  {
    id: "prov-local",
    providerType: "Local",
    displayName: "Local Model Execution",
    defaultModel: "mistral-7b-instruct",
    isEncrypted: false,
    isActive: false,
    priorityOrder: 4,
    totalTokensProcessed: 0,
    monthlyCostUsd: 0,
    healthStatus: "unhealthy",
    lastPingMs: 0,
  },
];

export function useIntelligenceData() {
  const queryClient = useQueryClient();

  const query = useQuery<AiProviderConfig[]>({
    queryKey: ["intelligenceProviders"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_PROVIDERS;
    },
    staleTime: 30000,
  });

  const testProviderMutation = useMutation({
    mutationFn: async (providerId: string) => {
      await new Promise((r) => setTimeout(r, 700));
      return { success: true, latencyMs: Math.floor(Math.random() * 40) + 80 };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["intelligenceProviders"] });
    },
  });

  return { ...query, testProviderMutation };
}
