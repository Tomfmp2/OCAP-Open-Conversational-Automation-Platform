import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface InboxConversation {
  id: string;
  channel: string;
  contactName: string;
  externalContactId: string | null;
  status: string;
  handledByHuman: boolean;
  canReply: boolean;
  lastActivityAt: string;
  lastMessage: string | null;
  lastSender: "User" | "Agent" | "Operator" | "System" | null;
}

export interface InboxMessage {
  id: string;
  sender: "User" | "Agent" | "Operator" | "System";
  text: string;
  createdAt: string;
}

/** Los mensajes llegan por webhooks: la bandeja se refresca sola mientras está abierta. */
const REFRESH_MS = 5000;
const INBOX_KEY = ["inbox"];

export function useInbox(onlyHuman: boolean) {
  return useQuery<InboxConversation[]>({
    queryKey: [...INBOX_KEY, { onlyHuman }],
    queryFn: () => apiClient.get<InboxConversation[]>(`/api/inbox${onlyHuman ? "?onlyHuman=true" : ""}`),
    refetchInterval: REFRESH_MS,
  });
}

export function useInboxMessages(conversationId: string | null) {
  return useQuery<InboxMessage[]>({
    queryKey: [...INBOX_KEY, "messages", conversationId],
    queryFn: () => apiClient.get<InboxMessage[]>(`/api/inbox/${conversationId}/messages`),
    enabled: conversationId !== null,
    refetchInterval: REFRESH_MS,
  });
}

/** Tomar, devolver al agente o responder como persona; refresca la bandeja al terminar. */
export function useInboxActions(conversationId: string) {
  const queryClient = useQueryClient();
  const refresh = () => queryClient.invalidateQueries({ queryKey: INBOX_KEY });

  const takeOver = useMutation({
    mutationFn: () => apiClient.post(`/api/inbox/${conversationId}/takeover`),
    onSuccess: refresh,
  });
  const release = useMutation({
    mutationFn: () => apiClient.post(`/api/inbox/${conversationId}/release`),
    onSuccess: refresh,
  });
  const reply = useMutation({
    mutationFn: (text: string) => apiClient.post(`/api/inbox/${conversationId}/reply`, { text }),
    onSuccess: refresh,
  });

  return { takeOver, release, reply };
}
