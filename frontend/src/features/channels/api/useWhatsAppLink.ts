"use client";

import React from "react";
import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import { getAccessToken } from "@/shared/api/tokenStorage";

export type WhatsAppLinkState = "unlinked" | "pending" | "linked";

export interface WhatsAppLinkStatus {
  state: WhatsAppLinkState;
  qrBase64?: string | null;
  pairingCode?: string | null;
  phoneNumber?: string | null;
  profileName?: string | null;
}

const LINK_URL = "/api/channels/whatsapp/link";
/** Evento que emite el webhook de Evolution por SignalR (EvolutionWhatsAppWebhookController.LinkUpdatedEvent). */
const LINK_UPDATED_EVENT = "WhatsAppLinkUpdated";
/** Solo si el hub no conecta: el QR de WhatsApp rota cada ~20 s, así que basta con consultar cada pocos segundos. */
const FALLBACK_POLL_MS = 5000;

/**
 * Vinculación del WhatsApp de la organización por QR. Mientras está activa escucha los eventos del webhook en vivo
 * (QR nuevo, número vinculado) y, si el hub en tiempo real no está disponible, consulta el estado como respaldo.
 */
export function useWhatsAppLink(active: boolean) {
  const queryClient = useQueryClient();
  const [status, setStatus] = React.useState<WhatsAppLinkStatus | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [busy, setBusy] = React.useState(false);
  const [live, setLive] = React.useState(false);

  const apply = React.useCallback(
    (next: WhatsAppLinkStatus) => {
      // Un evento sin QR (p. ej. "connecting") no debe borrar el QR que se está mostrando.
      setStatus((prev) =>
        next.state === "pending" && !next.qrBase64 && prev?.state === "pending" ? { ...prev, ...next, qrBase64: prev.qrBase64 } : next
      );
      if (next.state === "linked" || next.state === "unlinked") {
        void queryClient.invalidateQueries({ queryKey: ["channelsData"] });
      }
    },
    [queryClient]
  );

  const run = React.useCallback(async (request: () => Promise<WhatsAppLinkStatus | null>) => {
    setBusy(true);
    setError(null);
    try {
      const result = await request();
      if (result) apply(result);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "No se pudo contactar el servicio de WhatsApp.");
    } finally {
      setBusy(false);
    }
  }, [apply]);

  const link = React.useCallback(() => run(() => apiClient.post<WhatsAppLinkStatus>(LINK_URL, {})), [run]);

  const unlink = React.useCallback(
    () =>
      run(async () => {
        await apiClient.delete(LINK_URL);
        return { state: "unlinked" };
      }),
    [run]
  );

  // Estado inicial al abrir: ¿ya hay un número vinculado?
  React.useEffect(() => {
    if (!active) return;
    apiClient
      .get<WhatsAppLinkStatus>(LINK_URL)
      .then(apply)
      .catch((err: unknown) =>
        setError(err instanceof Error ? err.message : "No se pudo consultar el estado de WhatsApp.")
      );
  }, [active, apply]);

  // Eventos en vivo del webhook mientras el modal está abierto.
  React.useEffect(() => {
    if (!active) return;
    const connection = new HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL || ""}/hubs/events`, { accessTokenFactory: () => getAccessToken() || "" })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    connection.on(LINK_UPDATED_EVENT, (next: WhatsAppLinkStatus) => apply(next));
    connection.onreconnected(() => setLive(true));
    connection.onreconnecting(() => setLive(false));
    connection.onclose(() => setLive(false));
    connection
      .start()
      .then(() => setLive(connection.state === HubConnectionState.Connected))
      .catch(() => setLive(false));
    return () => {
      setLive(false);
      void connection.stop();
    };
  }, [active, apply]);

  // Respaldo sin tiempo real: se consulta el estado (con el QR vigente) solo mientras se espera el escaneo.
  const waiting = active && status?.state === "pending" && !live;
  React.useEffect(() => {
    if (!waiting) return;
    const timer = setInterval(() => {
      apiClient
        .get<WhatsAppLinkStatus>(`${LINK_URL}?qr=true`)
        .then(apply)
        .catch(() => undefined);
    }, FALLBACK_POLL_MS);
    return () => clearInterval(timer);
  }, [waiting, apply]);

  return { status, error, busy, live, link, unlink };
}
