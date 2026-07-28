import { useQuery } from "@tanstack/react-query";

export interface InstallerStep {
  id: number;
  title: string;
  description: string;
  status: "completed" | "current" | "pending" | "error";
  details: string;
}

export interface InstallerData {
  steps: InstallerStep[];
  isSystemReady: boolean;
}

export function useInstallerData() {
  return useQuery<InstallerData>({
    queryKey: ["installerData"],
    queryFn: async () => {
      const res = await fetch("/api/health");
      if (!res.ok) {
        return { steps: [], isSystemReady: false };
      }
      const data = await res.json();
      return {
        steps: data?.steps || [],
        isSystemReady: data?.isSystemReady ?? true,
      };
    },
    staleTime: 30000,
  });
}
