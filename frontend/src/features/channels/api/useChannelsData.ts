import React from "react";
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

function pickHealth(
  provider: string,
  tg: PromiseSettledResult<{ data?: ChannelProviderHealthDto }>,
  wa: PromiseSettledResult<{ data?: ChannelProviderHealthDto }>,
  wc: PromiseSettledResult<{ data?: ChannelProviderHealthDto }>
): ChannelProviderHealthDto | undefined {
  const p = provider.toLowerCase();
  if (p === "telegram" && tg.status === "fulfilled") return tg.value.data;
  if (p === "whatsapp" && wa.status === "fulfilled") return wa.value.data;
  if (p === "webchat" && wc.status === "fulfilled") return wc.value.data;
  return undefined;
}

export function useChannelsData() {
  const queryClient = useQueryClient();

  const query = useQuery<ChannelConnectionDto[]>({
    queryKey: ["channelsData"],
    queryFn: async () => {
      const [tgHealth, waHealth, wcHealth, dbConns] = await Promise.allSettled([
        apiClient.get<{ data?: ChannelProviderHealthDto }>("/api/channels/telegram/health"),
        apiClient.get<{ data?: ChannelProviderHealthDto }>("/api/channels/whatsapp/health"),
        apiClient.get<{ data?: ChannelProviderHealthDto }>("/api/channels/webchat/health"),
        apiClient.get<{ data?: unknown[] } | unknown[]>("/api/channels/connections"),
      ]);

      const connections: ChannelConnectionDto[] = [];

      if (dbConns.status === "fulfilled") {
        const raw = dbConns.value;
        const list = (raw as { data?: unknown[] })?.data || (Array.isArray(raw) ? raw : []);
        (
          list as Array<{
            id: string;
            displayName?: string;
            name?: string;
            provider: string;
            enabled?: boolean;
            accountIdentifier?: string;
            messagesHandled24h?: number;
            lastSyncAtUtc?: string;
          }>
        ).forEach((item) => {
          const health = pickHealth(item.provider, tgHealth, waHealth, wcHealth);
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
      const lower = channelProviderOrId.toLowerCase();
      const provider = lower.includes("whatsapp")
        ? "whatsapp"
        : lower.includes("webchat")
          ? "webchat"
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
      widgetTitle?: string;
    }) => {
      const provider = payload.provider.toLowerCase();
      let url = "/api/channels/connect";
      let bodyData: Record<string, unknown> = { provider: payload.provider };

      if (provider === "telegram") {
        url = "/api/channels/telegram/bots";
        bodyData = {
          displayName: payload.displayName,
          botToken: payload.botToken,
          connectionMode: payload.connectionMode ?? "polling",
        };
      } else if (provider === "whatsapp") {
        // Flujo principal: QR Evolution (gratis). Cloud sigue en POST /connect.
        url = "/api/channels/whatsapp/connect-qr";
        bodyData = {
          displayName: payload.displayName,
        };
      } else if (provider === "webchat") {
        url = "/api/channels/webchat/connect";
        bodyData = {
          displayName: payload.displayName,
          widgetTitle: payload.widgetTitle,
        };
      } else {
        throw new Error(
          `Proveedor ${payload.provider} no implementado. Disponibles: Telegram, WhatsApp, WebChat.`
        );
      }

      return apiClient.post(url, bodyData);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["channelsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });
}

export interface WhatsAppQrData {
  connectionId: string;
  instanceName: string;
  qrBase64?: string;
  qrCode?: string;
  pairingCode?: string;
  status?: string;
}

export interface WhatsAppEvolutionState {
  instanceName: string;
  state: string;
  isOpen: boolean;
}

export function useWhatsAppQrConnect() {
  const queryClient = useQueryClient();
  const [qrData, setQrData] = React.useState<WhatsAppQrData | null>(null);
  const [connectionState, setConnectionState] = React.useState<WhatsAppEvolutionState | null>(null);
  const [isConnecting, setIsConnecting] = React.useState(false);
  const [isPolling, setIsPolling] = React.useState(false);
  const pollRef = React.useRef<ReturnType<typeof setInterval> | null>(null);

  const stopPolling = React.useCallback(() => {
    if (pollRef.current) {
      clearInterval(pollRef.current);
      pollRef.current = null;
    }
    setIsPolling(false);
  }, []);

  const reset = React.useCallback(() => {
    stopPolling();
    setQrData(null);
    setConnectionState(null);
    setIsConnecting(false);
  }, [stopPolling]);

  const startPolling = React.useCallback(
    (instanceName: string) => {
      stopPolling();
      setIsPolling(true);
      pollRef.current = setInterval(() => {
        void (async () => {
          try {
            const res = await apiClient.get<{
              success?: boolean;
              data?: WhatsAppEvolutionState;
            }>(`/api/channels/whatsapp/evolution/state/${encodeURIComponent(instanceName)}`);
            const state = res?.data;
            if (state) {
              setConnectionState(state);
              if (state.isOpen) {
                stopPolling();
                queryClient.invalidateQueries({ queryKey: ["channelsData"] });
                queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
              }
            }
          } catch {
            // ignore transient poll errors
          }
        })();
      }, 2500);
    },
    [queryClient, stopPolling]
  );

  const connectQr = React.useCallback(
    async (displayName: string, instanceName?: string) => {
      setIsConnecting(true);
      try {
        const res = await apiClient.post<{
          success?: boolean;
          message?: string;
          data?: WhatsAppQrData;
        }>("/api/channels/whatsapp/connect-qr", {
          displayName,
          instanceName,
        });
        if (!res?.data?.instanceName) {
          throw new Error(res?.message || "No se pudo generar el QR de Evolution.");
        }
        setQrData(res.data);
        startPolling(res.data.instanceName);
        return res.data;
      } finally {
        setIsConnecting(false);
      }
    },
    [startPolling]
  );

  const refreshQr = React.useCallback(async () => {
    if (!qrData?.instanceName) return null;
    const res = await apiClient.get<{
      success?: boolean;
      data?: Partial<WhatsAppQrData> & { instanceName: string };
    }>(`/api/channels/whatsapp/qr/${encodeURIComponent(qrData.instanceName)}`);
    if (res?.data) {
      setQrData((prev) =>
        prev
          ? {
              ...prev,
              qrBase64: res.data?.qrBase64 ?? prev.qrBase64,
              qrCode: res.data?.qrCode ?? prev.qrCode,
              pairingCode: res.data?.pairingCode ?? prev.pairingCode,
              status: res.data?.status ?? prev.status,
            }
          : null
      );
    }
    return res?.data ?? null;
  }, [qrData?.instanceName]);

  React.useEffect(() => () => stopPolling(), [stopPolling]);

  return {
    connectQr,
    refreshQr,
    qrData,
    connectionState,
    isConnecting,
    isPolling,
    reset,
  };
}

