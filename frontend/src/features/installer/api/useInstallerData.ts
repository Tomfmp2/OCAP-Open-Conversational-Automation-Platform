import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface InstallerStep {
  id: number;
  title: string;
  description: string;
  status: "completed" | "current" | "pending" | "error";
  details: string;
}

export interface InstallerData {
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

export function useInstallerData() {
  return useQuery<InstallerData>({
    queryKey: ["installerData"],
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
    staleTime: 30000,
    retry: 2,
  });
}
