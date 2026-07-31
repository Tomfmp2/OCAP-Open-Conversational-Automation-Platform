import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import {
  InstallerSetupResponse,
  InstallerStatus,
  toSetupPayload,
  type InstallerFormState,
} from "../model/installerForm";
import { InstallerStep } from "./useInstallerData";

export interface InstallerDiagnosticData {
  status: string;
  steps: InstallerStep[];
  isSystemReady: boolean;
  timestamp: string;
}

interface InstallerDiagnosticResponse {
  status: string;
  isSystemReady: boolean;
  timestamp: string;
  steps: Array<{
    id: number;
    title: string;
    description: string;
    status: string;
    details: string;
  }>;
}

function mapStep(
  raw: InstallerDiagnosticResponse["steps"][number]
): InstallerStep {
  const statusRaw = raw.status.toLowerCase();
  const status: InstallerStep["status"] =
    statusRaw === "completed"
      ? "completed"
      : statusRaw === "error"
        ? "error"
        : statusRaw === "current"
          ? "current"
          : "pending";

  return {
    id: raw.id,
    title: raw.title,
    description: raw.description,
    status,
    details: raw.details,
  };
}

export function useInstallerStatus() {
  return useQuery<InstallerStatus>({
    queryKey: ["installerStatus"],
    queryFn: () =>
      apiClient.get<InstallerStatus>("/api/installer/status", { skipAuth: true }),
    staleTime: 10_000,
    retry: 1,
  });
}

export function useInstallerDiagnostic(enabled: boolean) {
  return useQuery<InstallerDiagnosticData>({
    queryKey: ["installerData"],
    enabled,
    queryFn: async () => {
      const data = await apiClient.get<InstallerDiagnosticResponse>(
        "/api/health/diagnostic",
        { skipAuth: true }
      );
      return {
        status: data.status,
        steps: data.steps.map(mapStep),
        isSystemReady: data.isSystemReady,
        timestamp: data.timestamp,
      };
    },
    staleTime: 30_000,
    retry: 2,
  });
}

export function useInstallerSetupMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (form: InstallerFormState) =>
      apiClient.post<InstallerSetupResponse>(
        "/api/installer/setup",
        toSetupPayload(form),
        { skipAuth: true }
      ),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["installerStatus"] });
      void queryClient.invalidateQueries({ queryKey: ["installerData"] });
    },
  });
}
