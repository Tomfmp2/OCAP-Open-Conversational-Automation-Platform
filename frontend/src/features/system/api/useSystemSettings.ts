import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

/** Ajustes de toda la instalación. Un valor null usa el del servidor. */
export interface SystemSettings {
  rateLimitingEnabled: boolean | null;
  requestsPerMinute: number | null;
  authAttemptsPerMinute: number | null;
  widgetMessagesPerMinute: number | null;
  widgetReadsPerMinute: number | null;
  sessionMinutes: number | null;
  allowedOrigins: string[];
}

/** Valores del servidor: los que rigen cuando un campo queda vacío. */
export type SystemDefaults = { [K in keyof SystemSettings]: NonNullable<SystemSettings[K]> };

export interface SystemSettingsResponse {
  settings: SystemSettings;
  defaults: SystemDefaults;
}

const key = ["system", "settings"];

export function useSystemSettings() {
  return useQuery<SystemSettingsResponse>({ queryKey: key, queryFn: () => apiClient.get("/api/system/settings") });
}

export function useSaveSystemSettings() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (settings: SystemSettings) => apiClient.put<SystemSettingsResponse>("/api/system/settings", settings),
    onSuccess: (saved) => queryClient.setQueryData(key, saved),
  });
}
