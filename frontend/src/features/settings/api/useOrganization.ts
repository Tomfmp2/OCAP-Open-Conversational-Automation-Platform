import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface Organization {
  name: string;
}

const key = ["organization"];

export function useOrganization() {
  return useQuery<Organization>({ queryKey: key, queryFn: () => apiClient.get<Organization>("/api/settings") });
}

export function useRenameOrganization() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => apiClient.put<Organization>("/api/settings", { name }),
    onSuccess: (organization) => queryClient.setQueryData(key, organization),
  });
}

export interface EmailSettings {
  host: string | null;
  port: number;
  username: string | null;
  from: string | null;
  useSsl: boolean;
  hasPassword: boolean;
}

const emailKey = ["organization", "email"];

/** SMTP propio de la organización; sin él, `usesServer` indica si el servidor tiene uno. */
export function useEmailSettings() {
  return useQuery<{ settings: EmailSettings | null; usesServer: boolean }>({
    queryKey: emailKey,
    queryFn: () => apiClient.get("/api/settings/email"),
    retry: false,
  });
}

export function useEmailSettingsActions() {
  const queryClient = useQueryClient();
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: emailKey });
    void queryClient.invalidateQueries({ queryKey: ["team"] });
  };
  return {
    save: useMutation({
      /** password: null conserva la guardada. */
      mutationFn: (m: { host: string; port: number; username: string; password: string | null; from: string; useSsl: boolean }) =>
        apiClient.put("/api/settings/email", m),
      onSuccess: refresh,
    }),
    remove: useMutation({ mutationFn: () => apiClient.delete("/api/settings/email"), onSuccess: refresh }),
    test: useMutation({ mutationFn: () => apiClient.post<{ sentTo: string }>("/api/settings/email/test", {}, { timeout: 45_000, skipRetry: true }) }),
  };
}

export interface Preferences {
  language: "es" | "en" | null;
  timeZone: string | null;
  panelUrl: string | null;
  conversationRetentionDays: number | null;
}

const preferencesKey = ["organization", "preferences"];

export function usePreferences() {
  return useQuery<Preferences>({ queryKey: preferencesKey, queryFn: () => apiClient.get("/api/settings/preferences") });
}

export function useSavePreferences() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (p: Preferences) => apiClient.put<Preferences>("/api/settings/preferences", p),
    onSuccess: (saved) => queryClient.setQueryData(preferencesKey, saved),
  });
}

const googleKey = ["organization", "google"];

/** Dirección a la que vuelve Google: hay que registrarla tal cual en Google Cloud Console. */
export const googleRedirectUri = () => `${window.location.origin}/integrations/google/callback`;

export function useGoogleSettings() {
  return useQuery<{ clientId: string | null; usesServer: boolean }>({
    queryKey: googleKey,
    queryFn: () => apiClient.get("/api/settings/google"),
    retry: false,
  });
}

export function useGoogleConnection(enabled: boolean) {
  return useQuery<{ isConnected: boolean; oAuthStatus: string; grantedScopes: string[] }>({
    queryKey: [...googleKey, "connection"],
    queryFn: () => apiClient.get("/api/integrations/google"),
    enabled,
  });
}

export function useGoogleActions() {
  const queryClient = useQueryClient();
  const refresh = () => void queryClient.invalidateQueries({ queryKey: googleKey });
  return {
    save: useMutation({
      mutationFn: (m: { clientId: string; clientSecret: string | null }) => apiClient.put("/api/settings/google", m),
      onSuccess: refresh,
    }),
    remove: useMutation({ mutationFn: () => apiClient.delete("/api/settings/google"), onSuccess: refresh }),
    /** Pide a la API la dirección de consentimiento (con state firmado) y lleva al usuario a Google. */
    connect: useMutation({
      mutationFn: async () => {
        const { url } = await apiClient.get<{ url: string }>(
          `/api/integrations/google/authorize-url?redirectUri=${encodeURIComponent(googleRedirectUri())}`
        );
        window.location.assign(url);
      },
    }),
    disconnect: useMutation({ mutationFn: () => apiClient.post("/api/integrations/google/disconnect", {}), onSuccess: refresh }),
  };
}
