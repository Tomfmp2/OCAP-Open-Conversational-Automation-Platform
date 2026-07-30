import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface ChannelProviderHealthDto {
  provider: string;
  health: string;
  latencyMs: number;
  lastPingAtUtc: string;
  errorMessage?: string;
}

export interface ChannelConnectionDto {
  id: string;
  name: string;
  provider: string;
  status: string;
  accountIdentifier: string;
  messagesHandled24h: number;
  lastSync: string;
  latencyMs: number;
  health?: ChannelProviderHealthDto;
}

export type ChannelConnection = ChannelConnectionDto;

export function useChannelsData() {
  const queryClient = useQueryClient();

  const query = useQuery<ChannelConnectionDto[]>({
    queryKey: ["channelsData"],
    queryFn: async () => {
      const [tgStatus, tgHealth, waStatus, waHealth, dbConns] = await Promise.allSettled([
        apiClient.get<{ data?: { status?: string } }>("/api/channels/telegram/status"),
        apiClient.get<{ data?: ChannelProviderHealthDto }>("/api/channels/telegram/health"),
        apiClient.get<{ data?: { status?: string } }>("/api/channels/whatsapp/status"),
        apiClient.get<{ data?: ChannelProviderHealthDto }>("/api/channels/whatsapp/health"),
        apiClient.get<{ data?: unknown[] } | unknown[]>("/api/channels/connections"),
      ]);

      const connections: ChannelConnectionDto[] = [];

      if (tgStatus.status === "fulfilled") {
        const statusJson = tgStatus.value;
        const healthJson = tgHealth.status === "fulfilled" ? tgHealth.value : null;

        connections.push({
          id: "telegram-channel-1",
          name: "Telegram Bot",
          provider: "Telegram",
          status: statusJson?.data?.status || "Connected",
          accountIdentifier: "telegram",
          messagesHandled24h: 0,
          lastSync: new Date().toLocaleTimeString(),
          latencyMs: healthJson?.data?.latencyMs ?? 0,
          health: healthJson?.data,
        });
      }

      if (waStatus.status === "fulfilled") {
        const statusJson = waStatus.value;
        const healthJson = waHealth.status === "fulfilled" ? waHealth.value : null;

        connections.push({
          id: "whatsapp-channel-1",
          name: "WhatsApp Business",
          provider: "WhatsApp",
          status: statusJson?.data?.status || "Connected",
          accountIdentifier: "whatsapp",
          messagesHandled24h: 0,
          lastSync: new Date().toLocaleTimeString(),
          latencyMs: healthJson?.data?.latencyMs ?? 0,
          health: healthJson?.data,
        });
      }

      if (dbConns.status === "fulfilled") {
        const raw = dbConns.value;
        const list = (raw as { data?: unknown[] })?.data || (Array.isArray(raw) ? raw : []);
        (list as Array<{
          id: string;
          displayName?: string;
          name?: string;
          provider: string;
          enabled?: boolean;
        }>).forEach((item) => {
            if (!connections.some((c) => c.provider.toLowerCase() === item.provider.toLowerCase())) {
              connections.push({
                id: item.id,
                name: item.displayName || item.name || `${item.provider} Channel`,
                provider: item.provider,
                status: item.enabled ? "Connected" : "Disconnected",
                accountIdentifier: `${item.provider.toLowerCase()}_account`,
                messagesHandled24h: 0,
                lastSync: new Date().toLocaleTimeString(),
                latencyMs: 0,
              });
            }
        });
      }

      return connections;
    },
    staleTime: 10000,
    retry: 2,
  });

  const testConnectionMutation = useMutation({
    mutationFn: async (channelProviderOrId: string) => {
      const provider = channelProviderOrId.toLowerCase().includes("telegram")
        ? "telegram"
        : channelProviderOrId.toLowerCase().includes("whatsapp")
          ? "whatsapp"
          : "telegram";

      const data = await apiClient.get<{ data?: unknown }>(`/api/channels/${provider}/health`);
      return { success: true, data: data?.data };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["channelsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  return { ...query, testConnectionMutation };
}

export function useConnectChannelMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: { provider: string; botToken?: string; authCode?: string }) => {
      let url = "/api/channels/connect";
      let bodyData: Record<string, unknown> = { provider: payload.provider };

      if (payload.provider.toLowerCase() === "telegram") {
        url = "/api/channels/telegram/bots";
        bodyData = { botToken: payload.botToken };
      } else if (payload.provider.toLowerCase() === "whatsapp") {
        url = "/api/channels/whatsapp/connect";
        bodyData = { phoneNumber: payload.botToken || "" };
      } else if (
        payload.provider.toLowerCase() === "google" ||
        payload.provider.toLowerCase() === "email"
      ) {
        url = "/api/integrations/google/connect";
        bodyData = { authCode: payload.authCode || "", scopes: "gmail,calendar" };
      }

      return apiClient.post(url, bodyData);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["channelsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });
}
