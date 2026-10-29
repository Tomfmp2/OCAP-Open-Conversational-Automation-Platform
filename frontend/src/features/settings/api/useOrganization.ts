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
