import { useQuery, useMutation } from "@tanstack/react-query";

export interface SettingsConfig {
  tenantName: string;
  defaultLocale: "es" | "en" | "de";
  timezone: string;
  auditLogRetentionDays: number;
  enableTelemetry: boolean;
  enableFailover: boolean;
}

export function useSettingsData() {
  const query = useQuery<SettingsConfig>({
    queryKey: ["settingsData"],
    queryFn: async () => {
      const res = await fetch("/api/settings");
      if (!res.ok) {
        return {
          tenantName: "OCAP Enterprise Tenant",
          defaultLocale: "es",
          timezone: "UTC",
          auditLogRetentionDays: 30,
          enableTelemetry: true,
          enableFailover: true,
        };
      }
      const data = await res.json();
      return data;
    },
    staleTime: 30000,
  });

  const updateSettingsMutation = useMutation({
    mutationFn: async (newConfig: SettingsConfig) => {
      const res = await fetch("/api/settings", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(newConfig),
      });
      if (!res.ok) return newConfig;
      return res.json();
    },
  });

  return { ...query, updateSettingsMutation };
}
