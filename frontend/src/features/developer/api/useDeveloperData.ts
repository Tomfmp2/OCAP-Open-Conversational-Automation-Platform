import { useQuery } from "@tanstack/react-query";

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
  url: string;
  events: string[];
  status: "active" | "failing";
  lastTriggered: string;
}

export interface DeveloperData {
  apiKeys: ApiKeyItem[];
  webhooks: WebhookItem[];
}

export function useDeveloperData() {
  return useQuery<DeveloperData>({
    queryKey: ["developerData"],
    queryFn: async () => {
      const res = await fetch("/api/apikeys");
      if (!res.ok) {
        return { apiKeys: [], webhooks: [] };
      }
      const data = await res.json();
      return {
        apiKeys: Array.isArray(data) ? data : data?.apiKeys || [],
        webhooks: data?.webhooks || [],
      };
    },
    staleTime: 30000,
  });
}
