import { useQuery, useMutation } from "@tanstack/react-query";

export interface SettingsConfig {
  tenantName: string;
  defaultLocale: "es" | "en" | "de";
  timezone: string;
  auditLogRetentionDays: number;
  enableTelemetry: boolean;
  enableFailover: boolean;
}

const DEFAULT_SETTINGS: SettingsConfig = {
  tenantName: "OCAP Enterprise Tenant",
  defaultLocale: "es",
  timezone: "UTC",
  auditLogRetentionDays: 30,
  enableTelemetry: true,
  enableFailover: true,
};

export function useSettingsData() {
  const query = useQuery<SettingsConfig>({
    queryKey: ["settingsData"],
    queryFn: async () => {
      try {
        if (typeof window === "undefined") return DEFAULT_SETTINGS;
        const res = await fetch("/api/settings");
        if (!res.ok) return DEFAULT_SETTINGS;
        const data = await res.json();
        return data || DEFAULT_SETTINGS;
      } catch {
        return DEFAULT_SETTINGS;
      }
    },
    staleTime: 30000,
  });

  const updateSettingsMutation = useMutation({
    mutationFn: async (newConfig: SettingsConfig) => {
      try {
        if (typeof window === "undefined") return newConfig;
        const res = await fetch("/api/settings", {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(newConfig),
        });
        if (!res.ok) return newConfig;
        return await res.json();
      } catch {
        return newConfig;
      }
    },
  });

  return { ...query, updateSettingsMutation };
}
