"use client";

import React from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface ChatLine {
  id: string;
  role: "user" | "assistant";
  text: string;
}

interface PrincipalReply {
  success?: boolean;
  message?: string;
  data?: { reply?: string; providerUsed?: string; conversationId?: string };
}

export interface AssistantConversation {
  id: string;
  title: string;
  updatedAt: string;
}

const CONVERSATIONS_KEY = ["assistantConversations"];
const BASE = "/api/agents/principal/conversations";

/** El agente puede encadenar herramientas (correo, calendario…): más margen que el timeout por defecto. */
const AGENT_TIMEOUT_MS = 120_000;

/**
 * Conversación con el agente principal. El historial vive en la base (por usuario): se puede retomar desde
 * cualquier dispositivo; aquí solo está la conversación abierta.
 */
export function usePrincipalAgentChat() {
  const queryClient = useQueryClient();
  const [conversationId, setConversationId] = React.useState<string | null>(null);
  const [lines, setLines] = React.useState<ChatLine[]>([]);
  const [pending, setPending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [providerUsed, setProviderUsed] = React.useState<string | null>(null);
  /** Respuesta que se está mostrando palabra por palabra (el orbe "habla" mientras tanto). */
  const [revealingId, setRevealingId] = React.useState<string | null>(null);

  const reset = React.useCallback(() => {
    setConversationId(null);
    setLines([]);
    setError(null);
    setProviderUsed(null);
    setRevealingId(null);
  }, []);

  const finishReveal = React.useCallback(() => setRevealingId(null), []);

  const open = React.useCallback(async (id: string) => {
    setError(null);
    setRevealingId(null);
    try {
      const messages = await apiClient.get<Array<{ id: string; role: ChatLine["role"]; content: string }>>(`${BASE}/${id}/messages`);
      setLines(messages.map((m) => ({ id: m.id, role: m.role, text: m.content })));
      setConversationId(id);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "No se pudo abrir la conversación.");
    }
  }, []);

  const send = React.useCallback(
    async (text: string) => {
      const trimmed = text.trim();
      if (!trimmed || pending) return;

      setError(null);
      setLines((prev) => [...prev, { id: `u-${Date.now()}`, role: "user", text: trimmed }]);
      setPending(true);

      try {
        const res = await apiClient.post<PrincipalReply>(
          "/api/agents/principal/messages",
          { conversationId, message: trimmed },
          { timeout: AGENT_TIMEOUT_MS }
        );

        const reply = res?.data?.reply || "Sin respuesta del agente.";
        if (res?.data?.providerUsed) setProviderUsed(res.data.providerUsed);
        if (res?.data?.conversationId) setConversationId(res.data.conversationId);
        void queryClient.invalidateQueries({ queryKey: CONVERSATIONS_KEY });
        const id = `a-${Date.now()}`;
        setLines((prev) => [...prev, { id, role: "assistant", text: String(reply) }]);
        setRevealingId(id);
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "No se pudo hablar con el agente principal.");
      } finally {
        setPending(false);
      }
    },
    [pending, conversationId, queryClient]
  );

  return { conversationId, lines, pending, error, providerUsed, revealingId, finishReveal, send, reset, open };
}

export function useAssistantConversations(enabled: boolean) {
  return useQuery<AssistantConversation[]>({ queryKey: CONVERSATIONS_KEY, queryFn: () => apiClient.get(BASE), enabled });
}

export function useAssistantConversationActions() {
  const queryClient = useQueryClient();
  const refresh = () => void queryClient.invalidateQueries({ queryKey: CONVERSATIONS_KEY });
  return {
    rename: useMutation({ mutationFn: (m: { id: string; title: string }) => apiClient.patch(`${BASE}/${m.id}`, { title: m.title }), onSuccess: refresh }),
    remove: useMutation({ mutationFn: (id: string) => apiClient.delete(`${BASE}/${id}`), onSuccess: refresh }),
  };
}
