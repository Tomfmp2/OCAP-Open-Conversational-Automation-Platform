import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

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
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";

      const [apiKeysRes, webhooksRes] = await Promise.allSettled([
        fetch(`${baseUrl}/api/apikeys`),
        fetch(`${baseUrl}/api/webhooks`),
      ]);

      let apiKeys: ApiKeyItem[] = [];
      if (apiKeysRes.status === "fulfilled" && apiKeysRes.value.ok) {
        const rawKeys = await apiKeysRes.value.json();
        const list = Array.isArray(rawKeys) ? rawKeys : rawKeys?.apiKeys || [];
        apiKeys = list.map((k: { id: string; name: string; prefix?: string; isRevoked?: boolean; createdAtUtc?: string; expiresAtUtc?: string }) => ({
          id: k.id,
          name: k.name || "API Key",
          keyPrefix: k.prefix || "ocap_live_",
          createdAt: k.createdAtUtc ? new Date(k.createdAtUtc).toLocaleString() : new Date().toLocaleDateString(),
          lastUsed: "Hace 15 min",
          status: k.isRevoked ? "revoked" : "active",
        }));
      }

      let webhooks: WebhookItem[] = [];
      if (webhooksRes.status === "fulfilled" && webhooksRes.value.ok) {
        const rawWebhooks = await webhooksRes.value.json();
        const list = Array.isArray(rawWebhooks) ? rawWebhooks : rawWebhooks?.webhooks || [];
        webhooks = list.map((w: { id: string; name?: string; targetUrl?: string; url?: string; subscribedEvents?: string | string[]; isActive?: boolean; createdAtUtc?: string }) => ({
          id: w.id,
          name: w.name || "Webhook Target",
          url: w.targetUrl || w.url || "https://example.com/webhook",
          events: typeof w.subscribedEvents === "string" ? w.subscribedEvents.split(",") : Array.isArray(w.subscribedEvents) ? w.subscribedEvents : ["*"],
          status: w.isActive ? "active" : "inactive",
          lastTriggered: w.createdAtUtc ? new Date(w.createdAtUtc).toLocaleString() : "Hace 1 hora",
        }));
      }

      return { apiKeys, webhooks };
    },
    staleTime: 10000,
    retry: 2,
  });

  const createApiKeyMutation = useMutation({
    mutationFn: async (name: string) => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/apikeys`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name }),
      });
      if (!res.ok) throw new Error("Fallo al crear API Key en el servidor");
      return await res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["developerData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  const revokeApiKeyMutation = useMutation({
    mutationFn: async (keyId: string) => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/apikeys/${keyId}`, {
        method: "DELETE",
      });
      if (!res.ok) throw new Error("Fallo al revocar API Key");
      return await res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["developerData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  const createWebhookMutation = useMutation({
    mutationFn: async ({ name, targetUrl, events }: { name: string; targetUrl: string; events: string[] }) => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/webhooks`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name, targetUrl, subscribedEvents: events }),
      });
      if (!res.ok) throw new Error("Fallo al crear Webhook en el servidor");
      return await res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["developerData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  const deleteWebhookMutation = useMutation({
    mutationFn: async (webhookId: string) => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/webhooks/${webhookId}`, {
        method: "DELETE",
      });
      if (!res.ok) throw new Error("Fallo al eliminar Webhook");
      return await res.json();
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
