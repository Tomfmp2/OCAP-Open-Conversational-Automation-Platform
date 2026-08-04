"use client";

import { useEffect, useState, useRef, useCallback } from "react";
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { getAccessToken } from "@/shared/api/tokenStorage";

export interface PlatformEventLog {
  id: string;
  eventName: string;
  payload: unknown;
  timestamp: string;
}

export function useSignalR(tenantId?: string) {
  const queryClient = useQueryClient();
  const [connectionState, setConnectionState] = useState<
    "Connecting" | "Connected" | "Reconnecting" | "Disconnected"
  >("Connecting");
  const [liveEvents, setLiveEvents] = useState<PlatformEventLog[]>([]);
  const connectionRef = useRef<HubConnection | null>(null);

  const handleEvent = useCallback(
    (eventName: string, payload: unknown) => {
      const newLog: PlatformEventLog = {
        id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
        eventName,
        payload,
        timestamp: new Date().toISOString(),
      };

      setLiveEvents((prev) => [newLog, ...prev.slice(0, 49)]);

      if (eventName.startsWith("Agent")) {
        queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
        queryClient.invalidateQueries({ queryKey: ["agentsData"] });
      } else if (eventName.startsWith("Message") || eventName.includes("Channel")) {
        queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
        queryClient.invalidateQueries({ queryKey: ["channelsData"] });
      } else {
        queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
      }
    },
    [queryClient]
  );

  useEffect(() => {
    if (typeof window === "undefined") return;

    const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
    const hubUrl = `${baseUrl}/hubs/events`;

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => getAccessToken() || "",
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          setConnectionState("Reconnecting");
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 10000);
        },
      })
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    const eventsToListen = [
      "AgentStarted",
      "AgentCompleted",
      "MessageReceived",
      "MessageSent",
    ];

    eventsToListen.forEach((evt) => {
      connection.on(evt, (data: unknown) => handleEvent(evt, data));
    });

    connection.on("ReceiveEvent", (evtName: string, data: unknown) =>
      handleEvent(evtName, data)
    );

    connection.onreconnected(async () => {
      setConnectionState("Connected");
      if (tenantId) {
        try {
          await connection.invoke("SubscribeTenant", tenantId);
        } catch {
          // hub may not expose tenant groups in all environments
        }
      }
      queryClient.invalidateQueries();
    });

    connection.onclose(() => {
      setConnectionState("Disconnected");
    });

    connection
      .start()
      .then(async () => {
        setConnectionState("Connected");
        if (tenantId) {
          try {
            await connection.invoke("SubscribeTenant", tenantId);
          } catch {
            // degrade gracefully
          }
        }
      })
      .catch(() => {
        setConnectionState("Disconnected");
      });

    return () => {
      void connection.stop();
    };
  }, [handleEvent, queryClient, tenantId]);

  const reconnect = useCallback(async () => {
    if (
      connectionRef.current &&
      connectionRef.current.state === HubConnectionState.Disconnected
    ) {
      setConnectionState("Connecting");
      try {
        await connectionRef.current.start();
        if (tenantId) {
          await connectionRef.current.invoke("SubscribeTenant", tenantId);
        }
        setConnectionState("Connected");
      } catch {
        setConnectionState("Disconnected");
      }
    }
  }, [tenantId]);

  return {
    connectionState,
    isConnected: connectionState === "Connected",
    liveEvents,
    reconnect,
  };
}
