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
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";
      const res = await fetch(`${baseUrl}/api/health`);
      if (!res.ok) {
        throw new Error(`Error en servidor (${res.status}): No se pudo obtener el diagnóstico del sistema`);
      }
      const data = await res.json();
      return {
        steps: data?.steps || data?.Steps || [],
        isSystemReady: data?.isSystemReady ?? data?.IsSystemReady ?? true,
      };
    },
    staleTime: 30000,
    retry: 2,
  });
}
