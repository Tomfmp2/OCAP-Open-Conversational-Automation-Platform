"use client";

import React from "react";
import { apiClient } from "@/shared/api/apiClient";

export interface ChatLine {
  id: string;
  role: "user" | "assistant";
  text: string;
}

interface PrincipalReply {
  success?: boolean;
  message?: string;
  data?: { reply?: string; providerUsed?: string };
}

/** El agente puede encadenar herramientas (correo, calendario…): más margen que el timeout por defecto. */
const AGENT_TIMEOUT_MS = 120_000;

function newSessionId() {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return `principal-${Date.now()}`;
}

/** Conversación con el agente principal: una sesión por conversación, con su historial en memoria. */
export function usePrincipalAgentChat() {
  const [sessionId, setSessionId] = React.useState(newSessionId);
  const [lines, setLines] = React.useState<ChatLine[]>([]);
  const [pending, setPending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [providerUsed, setProviderUsed] = React.useState<string | null>(null);

  const reset = React.useCallback(() => {
    setSessionId(newSessionId());
    setLines([]);
    setError(null);
    setProviderUsed(null);
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
          { sessionId, message: trimmed },
          { timeout: AGENT_TIMEOUT_MS }
        );

        const reply = res?.data?.reply || "Sin respuesta del agente.";
        if (res?.data?.providerUsed) setProviderUsed(res.data.providerUsed);
        setLines((prev) => [...prev, { id: `a-${Date.now()}`, role: "assistant", text: String(reply) }]);
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "No se pudo hablar con el agente principal.");
      } finally {
        setPending(false);
      }
    },
    [pending, sessionId]
  );

  return { lines, pending, error, providerUsed, send, reset };
}
