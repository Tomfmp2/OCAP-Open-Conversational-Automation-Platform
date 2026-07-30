import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

export interface SettingsConfig {
  tenantName: string;
  defaultLocale: "es" | "en" | "de";
  timezone: string;
  auditLogRetentionDays: number;
  enableTelemetry: boolean;
  enableFailover: boolean;
}

export function useSettingsData() {
  const queryClient = useQueryClient();

  const query = useQuery<SettingsConfig>({
    queryKey: ["settingsData"],
    queryFn: async () => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/settings`);
      if (!res.ok) {
        throw new Error(`Error (${res.status}): No se pudieron obtener los ajustes del tenant`);
      }
      return await res.json();
    },
    staleTime: 30000,
    retry: 2,
  });

  const updateSettingsMutation = useMutation({
    mutationFn: async (newConfig: SettingsConfig) => {
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/settings`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newConfig),
      });

      if (!res.ok) {
        const errorText = await res.text();
        throw new Error(errorText || `Error (${res.status}): No se pudieron guardar los ajustes`);
      }

      return await res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["settingsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  return { ...query, updateSettingsMutation };
}
