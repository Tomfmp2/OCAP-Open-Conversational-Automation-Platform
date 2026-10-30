import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface MfaStatus {
  enabled: boolean;
  recoveryCodesLeft: number;
}

const MFA_KEY = ["mfaStatus"];

export function useMfaStatus() {
  return useQuery({ queryKey: MFA_KEY, queryFn: () => apiClient.get<MfaStatus>("/api/auth/mfa/status") });
}

export function useAccountActions() {
  const queryClient = useQueryClient();
  const refresh = () => queryClient.invalidateQueries({ queryKey: MFA_KEY });

  return {
    changePassword: useMutation({
      mutationFn: (payload: { currentPassword: string; newPassword: string }) =>
        apiClient.post("/api/profile/change-password", payload, { skipRetry: true }),
    }),
    startMfa: useMutation({
      mutationFn: () => apiClient.post<{ secret: string; qrCodeUri: string }>("/api/auth/mfa/setup", {}),
    }),
    enableMfa: useMutation({
      mutationFn: (code: string) => apiClient.post<{ recoveryCodes: string[] }>("/api/auth/mfa/enable", { code }, { skipRetry: true }),
      onSuccess: refresh,
    }),
    disableMfa: useMutation({
      mutationFn: (code: string) => apiClient.post("/api/auth/mfa/disable", { code }, { skipRetry: true }),
      onSuccess: refresh,
    }),
    regenerateCodes: useMutation({
      mutationFn: () => apiClient.post<string[]>("/api/auth/mfa/recovery-codes/regenerate", {}),
      onSuccess: refresh,
    }),
  };
}
