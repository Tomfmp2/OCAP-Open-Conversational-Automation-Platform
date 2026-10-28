import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import { getTenantId } from "@/shared/api/tokenStorage";

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
  /** Agente que responde en este canal; null = agente de atención por defecto. */
  agentId: string | null;
  /** Proveedor de IA (configuración de «IA y modelos») del canal; null = el activo de la organización. */
  aiProviderConfigId: string | null;
  /** Solo WebChat: lo necesario para incrustar el widget en el sitio del cliente. */
  widget?: WebChatWidget;
}

export interface WebChatWidget {
  publicKey: string;
  title: string;
  allowedOrigins: string[];
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
            configurationMetadata?: Record<string, string>;
          }>
        ).forEach((item) => {
          const health = pickHealth(item.provider, tgHealth, waHealth, wcHealth);
          connections.push({
            id: item.id,
            name: item.displayName || item.name || `${item.provider} Channel`,
            provider: item.provider,
            // WhatsApp por QR guarda su estado real (pending/linked/unlinked); habilitada no implica vinculada.
            status: !item.enabled
              ? "Disconnected"
              : item.configurationMetadata?.Status === "pending"
                ? "Pending"
                : item.configurationMetadata?.Status === "unlinked"
                  ? "Disconnected"
                  : "Connected",
            accountIdentifier: item.accountIdentifier || "—",
            messagesHandled24h: item.messagesHandled24h ?? 0,
            lastSync: item.lastSyncAtUtc
              ? new Date(item.lastSyncAtUtc).toLocaleString()
              : "N/D",
            latencyMs: health?.latencyMs ?? 0,
            health,
            agentId: item.configurationMetadata?.AgentId ?? null,
            aiProviderConfigId: item.configurationMetadata?.AiProviderConfigId ?? null,
            widget:
              item.provider.toLowerCase() === "webchat" && item.configurationMetadata?.PublicKey
                ? {
                    publicKey: item.configurationMetadata.PublicKey,
                    title: item.configurationMetadata.WidgetTitle ?? "",
                    allowedOrigins: (item.configurationMetadata.AllowedOrigins ?? "").split(",").filter(Boolean),
                  }
                : undefined,
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
      } else if (provider === "webchat") {
        url = "/api/channels/webchat/connect";
        bodyData = {
          displayName: payload.displayName,
          widgetTitle: payload.widgetTitle,
        };
      } else {
        throw new Error(
          `Proveedor ${payload.provider} no implementado. Disponibles: Telegram y WebChat (WhatsApp se vincula por QR).`
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

/** Asigna el agente que responde en un canal (null = agente de atención por defecto). */
export function useAssignChannelAgent() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ connectionId, agentId }: { connectionId: string; agentId: string | null }) =>
      apiClient.put(`/api/channels/connections/${connectionId}/agent`, { agentId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["channelsData"] }),
  });
}

export interface AiProviderOption {
  id: string;
  label: string;
  enabled: boolean;
}

/** Proveedores de IA registrados en «IA y modelos» para la organización. */
export function useAiProviderOptions() {
  return useQuery<AiProviderOption[]>({
    queryKey: ["intelligenceProviders", "options"],
    queryFn: async () => {
      const tenantId = getTenantId();
      if (!tenantId) return [];
      const configs = await apiClient.get<
        Array<{ id: string; displayName: string; providerName: string; modelName: string; isEnabled: boolean }>
      >(`/api/aiproviderconfigurations/tenant/${tenantId}`);
      return configs.map((c) => ({
        id: c.id,
        label: `${c.displayName || c.providerName} · ${c.modelName}`,
        enabled: c.isEnabled,
      }));
    },
    staleTime: 30_000,
  });
}

/** Asigna el proveedor de IA de un canal (null = el activo de la organización). */
export function useAssignChannelAi() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ connectionId, aiProviderConfigId }: { connectionId: string; aiProviderConfigId: string | null }) =>
      apiClient.put(`/api/channels/connections/${connectionId}/ai`, { aiProviderConfigId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["channelsData"] }),
  });
}

/** Cambia el nombre visible de un canal. */
export function useRenameChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ connectionId, displayName }: { connectionId: string; displayName: string }) =>
      apiClient.patch(`/api/channels/connections/${connectionId}`, { displayName }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["channelsData"] }),
  });
}

/** Elimina un canal; la API limpia el servicio externo (desvincula WhatsApp, quita el webhook de Telegram). */
export function useDeleteChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (connectionId: string) => apiClient.delete(`/api/channels/${connectionId}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["channelsData"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
      void queryClient.invalidateQueries({ queryKey: ["inbox"] });
    },
  });
}

/** Título y dominios (orígenes) donde puede incrustarse el widget de WebChat. */
export function useConfigureWidget() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ connectionId, widgetTitle, allowedOrigins }: { connectionId: string; widgetTitle: string; allowedOrigins: string[] }) =>
      apiClient.put(`/api/channels/webchat/${connectionId}/widget`, { widgetTitle, allowedOrigins }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["channelsData"] }),
  });
}

/** Clave pública nueva para el widget: la anterior deja de funcionar en los sitios que la usen. */
export function useRotateWidgetKey() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (connectionId: string) => apiClient.post<{ publicKey: string }>(`/api/channels/webchat/${connectionId}/widget/key`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["channelsData"] }),
  });
}
