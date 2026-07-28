import { useQuery, useMutation } from "@tanstack/react-query";

export interface SettingsConfig {
  tenantName: string;
  defaultLocale: "es" | "en" | "de";
  timezone: string;
  auditLogRetentionDays: number;
  enableTelemetry: boolean;
  enableFailover: boolean;
}

const MOCK_SETTINGS: SettingsConfig = {
  tenantName: "OCAP Enterprise HQ",
  defaultLocale: "es",
  timezone: "America/Bogota (UTC-5)",
  auditLogRetentionDays: 90,
  enableTelemetry: true,
  enableFailover: true,
};

export function useSettingsData() {
  const query = useQuery<SettingsConfig>({
    queryKey: ["settingsData"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_SETTINGS;
    },
    staleTime: 30000,
  });

  const updateSettingsMutation = useMutation({
    mutationFn: async (newConfig: SettingsConfig) => {
      await new Promise((r) => setTimeout(r, 500));
      return newConfig;
    },
  });

  return { ...query, updateSettingsMutation };
}
