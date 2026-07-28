import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

export interface ChannelConnectionDto {
  id: string;
  name: string;
  provider: string;
  status: string;
  accountIdentifier: string;
  messagesHandled24h: number;
  lastSync: string;
  latencyMs: number;
}

export type ChannelConnection = ChannelConnectionDto;

export function useChannelsData() {
  const queryClient = useQueryClient();

  const query = useQuery<ChannelConnectionDto[]>({
    queryKey: ["channelsData"],
    queryFn: async () => {
      try {
        if (typeof window === "undefined") return [];
        const res = await fetch("/api/channels/connections");
        if (!res.ok) return [];
        const data = await res.json();
        return data?.data || (Array.isArray(data) ? data : []);
      } catch {
        return [];
      }
    },
    staleTime: 15000,
  });

  const testConnectionMutation = useMutation({
    mutationFn: async (channelId: string) => {
      try {
        if (typeof window === "undefined") return { success: false };
        const res = await fetch(`/api/channels/connections/${channelId}/health`);
        if (!res.ok) return { success: false };
        return await res.json();
      } catch {
        return { success: false };
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["channelsData"] });
    },
  });

  return { ...query, testConnectionMutation };
}
