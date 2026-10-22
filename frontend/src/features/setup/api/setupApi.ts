import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import type { LoginResponse } from "@/features/auth/api/authApi";
import type { toSetupPayload } from "../model/setupForm";

export interface SetupStatus {
  /** Pendiente mientras la instalación no tiene ningún usuario. */
  required: boolean;
}

export const setupApi = {
  status(): Promise<SetupStatus> {
    return apiClient.get<SetupStatus>("/api/setup/status", { skipAuth: true });
  },

  /** Crea la organización y el administrador; devuelve la sesión ya iniciada. */
  complete(payload: ReturnType<typeof toSetupPayload>): Promise<LoginResponse> {
    return apiClient.post<LoginResponse>("/api/setup", payload, { skipAuth: true, skipRetry: true });
  },
};

export function useSetupStatus() {
  return useQuery({
    queryKey: ["setupStatus"],
    queryFn: setupApi.status,
    staleTime: 0,
    retry: 1,
  });
}
