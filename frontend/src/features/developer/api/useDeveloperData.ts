import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface ApiKeyItem {
  id: string;
  name: string;
  keyPrefix: string;
  createdAt: string;
  lastUsed: string;
  status: "active" | "revoked";
}

export interface WebhookItem {
  id: string;
  name: string;
  url: string;
  events: string[];
  status: "active" | "failing" | "inactive";
  lastTriggered: string;
}

export interface DeveloperData {
  apiKeys: ApiKeyItem[];
  webhooks: WebhookItem[];
}

export function useDeveloperData() {
  const queryClient = useQueryClient();

  const query = useQuery<DeveloperData>({
    queryKey: ["developerData"],
    queryFn: async () => {
      const [rawKeys, rawWebhooks] = await Promise.allSettled([
        apiClient.get<unknown[] | { apiKeys?: unknown[] }>("/api/apikeys"),
        apiClient.get<unknown[] | { webhooks?: unknown[] }>("/api/webhooks"),
      ]);

      let apiKeys: ApiKeyItem[] = [];
      if (rawKeys.status === "fulfilled") {
        const list = Array.isArray(rawKeys.value)
          ? rawKeys.value
          : rawKeys.value?.apiKeys || [];
        apiKeys = (list as Array<{
          id: string;
          name: string;
          prefix?: string;
          isRevoked?: boolean;
          createdAtUtc?: string;
          lastUsedAtUtc?: string;
        }>).map((k) => ({
            id: k.id,
            name: k.name || "API Key",
            keyPrefix: k.prefix || "ocap_live_",
            createdAt: k.createdAtUtc
              ? new Date(k.createdAtUtc).toLocaleString()
              : new Date().toLocaleDateString(),
            lastUsed: k.lastUsedAtUtc
              ? new Date(k.lastUsedAtUtc).toLocaleString()
              : "—",
            status: k.isRevoked ? "revoked" : "active",
          }));
      }

      let webhooks: WebhookItem[] = [];
      if (rawWebhooks.status === "fulfilled") {
        const list = Array.isArray(rawWebhooks.value)
          ? rawWebhooks.value
          : rawWebhooks.value?.webhooks || [];
        webhooks = (list as Array<{
          id: string;
          name?: string;
          targetUrl?: string;
          url?: string;
          subscribedEvents?: string | string[];
          isActive?: boolean;
          createdAtUtc?: string;
        }>).map((w) => ({
            id: w.id,
            name: w.name || "Webhook Target",
            url: w.targetUrl || w.url || "—",
            events:
              typeof w.subscribedEvents === "string"
                ? w.subscribedEvents.split(",")
                : Array.isArray(w.subscribedEvents)
                  ? w.subscribedEvents
                  : ["*"],
            status: w.isActive ? "active" : "inactive",
            lastTriggered: w.createdAtUtc
              ? new Date(w.createdAtUtc).toLocaleString()
              : "—",
          }));
      }

      return { apiKeys, webhooks };
    },
    staleTime: 10000,
    retry: 2,
  });

  const createApiKeyMutation = useMutation({
    mutationFn: async (name: string) => {
      return apiClient.post("/api/apikeys", { name });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["developerData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  const revokeApiKeyMutation = useMutation({
    mutationFn: async (keyId: string) => {
      return apiClient.delete(`/api/apikeys/${keyId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["developerData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  const createWebhookMutation = useMutation({
    mutationFn: async ({
      name,
      targetUrl,
      events,
    }: {
      name: string;
      targetUrl: string;
      events: string[];
    }) => {
      return apiClient.post("/api/webhooks", { name, targetUrl, subscribedEvents: events });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["developerData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  const deleteWebhookMutation = useMutation({
    mutationFn: async (webhookId: string) => {
      return apiClient.delete(`/api/webhooks/${webhookId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["developerData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  return {
    ...query,
    createApiKeyMutation,
    revokeApiKeyMutation,
    createWebhookMutation,
    deleteWebhookMutation,
  };
}
