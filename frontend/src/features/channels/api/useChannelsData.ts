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
      const [tgHealth, waHealth, dbConns] = await Promise.allSettled([
        apiClient.get<{ data?: ChannelProviderHealthDto }>("/api/channels/telegram/health"),
        apiClient.get<{ data?: ChannelProviderHealthDto }>("/api/channels/whatsapp/health"),
        apiClient.get<{ data?: unknown[] } | unknown[]>("/api/channels/connections"),
      ]);

      const connections: ChannelConnectionDto[] = [];

      if (dbConns.status === "fulfilled") {
        const raw = dbConns.value;
        const list = (raw as { data?: unknown[] })?.data || (Array.isArray(raw) ? raw : []);
        (list as Array<{
          id: string;
          displayName?: string;
          name?: string;
          provider: string;
          enabled?: boolean;
          accountIdentifier?: string;
          messagesHandled24h?: number;
          lastSyncAtUtc?: string;
        }>).forEach((item) => {
          const provider = item.provider.toLowerCase();
          const health =
            provider === "telegram" && tgHealth.status === "fulfilled"
              ? tgHealth.value.data
              : provider === "whatsapp" && waHealth.status === "fulfilled"
                ? waHealth.value.data
                : undefined;

          connections.push({
            id: item.id,
            name: item.displayName || item.name || `${item.provider} Channel`,
            provider: item.provider,
            status: item.enabled ? "Connected" : "Disconnected",
            accountIdentifier: item.accountIdentifier || "—",
            messagesHandled24h: item.messagesHandled24h ?? 0,
            lastSync: item.lastSyncAtUtc
              ? new Date(item.lastSyncAtUtc).toLocaleString()
              : "N/D",
            latencyMs: health?.latencyMs ?? 0,
            health,
          });
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
    mutationFn: async (payload: {
      provider: string;
      displayName: string;
      botToken?: string;
      connectionMode?: "webhook" | "polling";
      phoneNumberId?: string;
      apiToken?: string;
    }) => {
      let url = "/api/channels/connect";
      let bodyData: Record<string, unknown> = { provider: payload.provider };

      if (payload.provider.toLowerCase() === "telegram") {
        url = "/api/channels/telegram/bots";
        bodyData = {
          displayName: payload.displayName,
          botToken: payload.botToken,
          connectionMode: payload.connectionMode ?? "polling",
        };
      } else if (payload.provider.toLowerCase() === "whatsapp") {
        url = "/api/channels/whatsapp/connect";
        bodyData = {
          displayName: payload.displayName,
          phoneNumberId: payload.phoneNumberId,
          apiToken: payload.apiToken,
        };
      } else {
        throw new Error(`Proveedor ${payload.provider} no soportado por este formulario.`);
      }

      return apiClient.post(url, bodyData);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["channelsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });
}
