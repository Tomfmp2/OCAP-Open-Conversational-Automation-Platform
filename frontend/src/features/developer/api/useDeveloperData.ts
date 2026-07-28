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

const MOCK_DEVELOPER_DATA: DeveloperData = {
  apiKeys: [
    {
      id: "key-1",
      name: "Production API Key (Backend SDK)",
      keyPrefix: "ocap_live_948f...",
      createdAt: "2026-06-15",
      lastUsed: "Hace 2 min",
      status: "active",
    },
    {
      id: "key-2",
      name: "Staging Test Key",
      keyPrefix: "ocap_test_102a...",
      createdAt: "2026-07-01",
      lastUsed: "Hace 1 hora",
      status: "active",
    },
  ],
  webhooks: [
    {
      id: "wh-1",
      url: "https://api.enterprise.com/webhooks/ocap-events",
      events: ["agent.execution.completed", "channel.message.received"],
      status: "active",
      lastTriggered: "Hace 5 min",
    },
  ],
};

export function useDeveloperData() {
  return useQuery<DeveloperData>({
    queryKey: ["developerData"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_DEVELOPER_DATA;
    },
    staleTime: 30000,
  });
}
