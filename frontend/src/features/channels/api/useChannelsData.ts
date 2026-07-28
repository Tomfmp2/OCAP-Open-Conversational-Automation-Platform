import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

export interface ChannelConnection {
  id: string;
  name: string;
  provider: "Telegram" | "WhatsApp" | "Google" | "Slack";
  status: "connected" | "disconnected" | "error" | "configuring";
  accountIdentifier: string;
  messagesHandled24h: number;
  lastSync: string;
  latencyMs: number;
}

const MOCK_CHANNELS: ChannelConnection[] = [
  {
    id: "ch-tg-1",
    name: "Telegram Bot Corporativo",
    provider: "Telegram",
    status: "connected",
    accountIdentifier: "@OCAP_Assistant_Bot",
    messagesHandled24h: 4210,
    lastSync: "Hace 12 seg",
    latencyMs: 85,
  },
  {
    id: "ch-wa-1",
    name: "WhatsApp Business Cloud",
    provider: "WhatsApp",
    status: "connected",
    accountIdentifier: "+57 300 987 6543",
    messagesHandled24h: 3890,
    lastSync: "Hace 30 seg",
    latencyMs: 120,
  },
  {
    id: "ch-gg-1",
    name: "Google Workspace Integration",
    provider: "Google",
    status: "connected",
    accountIdentifier: "workspace@ocap-enterprise.com",
    messagesHandled24h: 1450,
    lastSync: "Hace 2 min",
    latencyMs: 140,
  },
  {
    id: "ch-sl-1",
    name: "Slack Enterprise Grid",
    provider: "Slack",
    status: "disconnected",
    accountIdentifier: "slack-bot-id-990",
    messagesHandled24h: 0,
    lastSync: "Desconectado",
    latencyMs: 0,
  },
];

export function useChannelsData() {
  const queryClient = useQueryClient();

  const query = useQuery<ChannelConnection[]>({
    queryKey: ["channelsData"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_CHANNELS;
    },
    staleTime: 30000,
  });

  const testConnectionMutation = useMutation({
    mutationFn: async (channelId: string) => {
      await new Promise((r) => setTimeout(r, 600));
      return { success: true, latencyMs: Math.floor(Math.random() * 50) + 70 };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["channelsData"] });
    },
  });

  return { ...query, testConnectionMutation };
}
