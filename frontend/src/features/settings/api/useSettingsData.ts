import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

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
      return apiClient.get<SettingsConfig>("/api/settings");
    },
    staleTime: 30000,
    retry: 2,
  });

  const updateSettingsMutation = useMutation({
    mutationFn: async (newConfig: SettingsConfig) => {
      return apiClient.put<SettingsConfig>("/api/settings", newConfig);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["settingsData"] });
      queryClient.invalidateQueries({ queryKey: ["dashboardOverview"] });
    },
  });

  return { ...query, updateSettingsMutation };
}
