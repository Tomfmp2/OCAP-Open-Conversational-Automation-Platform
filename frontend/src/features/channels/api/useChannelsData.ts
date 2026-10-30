import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import { getTenantId } from "@/shared/api/tokenStorage";

export type ChannelStatus = "Connected" | "Pending" | "Disconnected";

export interface ChannelConnection {
  id: string;
  name: string;
  provider: "Telegram" | "WhatsApp" | "WebChat" | string;
  status: ChannelStatus;
  /** Número, usuario del bot o dominio, según el canal. */
  accountIdentifier: string | null;
  /** Agente que responde en este canal; null = agente de atención por defecto. */
  agentId: string | null;
  /** Proveedor de IA (de «Modelos de IA») del canal; null = el activo de la organización. */
  aiProviderConfigId: string | null;
  /** Solo WebChat: lo necesario para incrustar el widget en el sitio del cliente. */
  widget?: WebChatWidget;
  /** on: responde; stopping: termina los chats en curso (5 min sin mensajes) y se apaga; off: no responde. */
  power: "on" | "stopping" | "off";
  /** Colecciones que consulta el agente en este canal ([] = todas); null = aún no se eligió (usa las del agente). */
  knowledgeBaseIds: string[] | null;
  /** Acciones que puede hacer el agente en este canal; null = aún no se eligió (usa las del agente). */
  tools: string[] | null;
}

export interface WebChatWidget {
  publicKey: string;
  title: string;
  allowedOrigins: string[];
}

interface ConnectionResponse {
  id: string;
  displayName?: string;
  provider: string;
  enabled?: boolean;
  accountIdentifier?: string;
  configurationMetadata?: Record<string, string>;
}

const CHANNELS_KEY = ["channels"];

function toChannel(item: ConnectionResponse): ChannelConnection {
  const meta = item.configurationMetadata ?? {};
  // WhatsApp por QR guarda su estado real (pending/linked/unlinked): habilitada no implica vinculada.
  const status: ChannelStatus = !item.enabled || meta.Status === "unlinked" ? "Disconnected" : meta.Status === "pending" ? "Pending" : "Connected";
  return {
    id: item.id,
    name: item.displayName || item.provider,
    provider: item.provider,
    status,
    accountIdentifier: item.accountIdentifier || null,
    agentId: meta.AgentId ?? null,
    aiProviderConfigId: meta.AiProviderConfigId ?? null,
    power: !item.enabled ? "off" : meta.ShuttingDownSince ? "stopping" : "on",
    knowledgeBaseIds: meta.KnowledgeBaseIds === undefined ? null : meta.KnowledgeBaseIds.split(",").filter(Boolean),
    tools: meta.Tools === undefined ? null : meta.Tools.split(",").filter(Boolean),
    widget:
      item.provider.toLowerCase() === "webchat" && meta.PublicKey
        ? { publicKey: meta.PublicKey, title: meta.WidgetTitle ?? "", allowedOrigins: (meta.AllowedOrigins ?? "").split(",").filter(Boolean) }
        : undefined,
  };
}

export function useChannels() {
  return useQuery<ChannelConnection[]>({
    queryKey: CHANNELS_KEY,
    queryFn: async () => {
      const response = await apiClient.get<{ data?: ConnectionResponse[] } | ConnectionResponse[]>("/api/channels/connections");
      const list = Array.isArray(response) ? response : (response.data ?? []);
      return list.map(toChannel);
    },
  });
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
      queryClient.invalidateQueries({ queryKey: CHANNELS_KEY });
    },
  });
}

/** Asigna el agente que responde en un canal (null = agente de atención por defecto). */
export function useAssignChannelAgent() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ connectionId, agentId }: { connectionId: string; agentId: string | null }) =>
      apiClient.put(`/api/channels/connections/${connectionId}/agent`, { agentId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CHANNELS_KEY }),
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
        label: `${c.displayName || c.providerName} (${c.modelName})`,
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
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CHANNELS_KEY }),
  });
}

/** Cambia el nombre visible de un canal. */
export function useRenameChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ connectionId, displayName }: { connectionId: string; displayName: string }) =>
      apiClient.patch(`/api/channels/connections/${connectionId}`, { displayName }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CHANNELS_KEY }),
  });
}

/** Elimina un canal; la API limpia el servicio externo (desvincula WhatsApp, quita el webhook de Telegram). */
export function useDeleteChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (connectionId: string) => apiClient.delete(`/api/channels/${connectionId}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CHANNELS_KEY });
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
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CHANNELS_KEY }),
  });
}

/** Clave pública nueva para el widget: la anterior deja de funcionar en los sitios que la usen. */
export function useRotateWidgetKey() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (connectionId: string) => apiClient.post<{ publicKey: string }>(`/api/channels/webchat/${connectionId}/widget/key`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CHANNELS_KEY }),
  });
}

/** Apagar (gradual: termina los chats en curso) o volver a encender un canal. */
export function useChannelPower() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (m: { id: string; on: boolean }) => apiClient.post(`/api/channels/${m.id}/${m.on ? "enable" : "shutdown"}`, {}),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: CHANNELS_KEY }),
  });
}

/** Conocimiento y acciones que el canal le da a su agente. */
export function useChannelCapabilities() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (m: { id: string; knowledgeBaseIds: string[]; tools: string[] }) =>
      apiClient.put(`/api/channels/connections/${m.id}/capabilities`, { knowledgeBaseIds: m.knowledgeBaseIds, tools: m.tools }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: CHANNELS_KEY }),
  });
}
