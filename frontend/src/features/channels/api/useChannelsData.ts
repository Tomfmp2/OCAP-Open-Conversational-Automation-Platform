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
      const res = await fetch("/api/channels/connections");
      if (!res.ok) {
        return [];
      }
      const data = await res.json();
      return data?.data || data || [];
    },
    staleTime: 15000,
  });

  const testConnectionMutation = useMutation({
    mutationFn: async (channelId: string) => {
      const res = await fetch(`/api/channels/connections/${channelId}/health`);
      if (!res.ok) throw new Error("Error testing connection health");
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["channelsData"] });
    },
  });

  return { ...query, testConnectionMutation };
}
