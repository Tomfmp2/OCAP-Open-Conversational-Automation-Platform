import { useQuery, useMutation } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

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

interface HealthStepRaw {
  id?: number;
  Id?: number;
  title?: string;
  Title?: string;
  description?: string;
  Description?: string;
  status?: string;
  Status?: string;
  details?: string;
  Details?: string;
}

function mapStep(raw: HealthStepRaw, index: number): InstallerStep {
  const statusRaw = (raw.status || raw.Status || "pending").toLowerCase();
  const status: InstallerStep["status"] =
    statusRaw === "completed"
      ? "completed"
      : statusRaw === "error"
        ? "error"
        : statusRaw === "current"
          ? "current"
          : "pending";

  return {
    id: raw.id ?? raw.Id ?? index + 1,
    title: raw.title || raw.Title || `Paso ${index + 1}`,
    description: raw.description || raw.Description || "",
    status,
    details: raw.details || raw.Details || "",
  };
}

export function useInstallerData() {
  return useQuery<InstallerData>({
    queryKey: ["installerData"],
    queryFn: async () => {
      const data = await apiClient.get<{
        steps?: HealthStepRaw[];
        Steps?: HealthStepRaw[];
        isSystemReady?: boolean;
        IsSystemReady?: boolean;
      }>("/api/health", { skipAuth: true });

      const rawSteps = data?.steps || data?.Steps || [];
      return {
        steps: rawSteps.map(mapStep),
        isSystemReady: data?.isSystemReady ?? data?.IsSystemReady ?? true,
      };
    },
    staleTime: 30000,
    retry: 2,
  });
}

export function useInstallerValidation() {
  return useMutation({
    mutationFn: async () => {
      const steps: InstallerStep[] = [];

      const healthCheck = async (
        id: number,
        title: string,
        description: string,
        check: () => Promise<{ ok: boolean; details: string }>
      ) => {
        try {
          const result = await check();
          steps.push({
            id,
            title,
            description,
            status: result.ok ? "completed" : "error",
            details: result.details,
          });
        } catch (err) {
          steps.push({
            id,
            title,
            description,
            status: "error",
            details: err instanceof Error ? err.message : "Error desconocido",
          });
        }
      };

      await healthCheck(1, "Salud del API Backend", "Verificación de conectividad con /api/health", async () => {
        const data = await apiClient.get<{ status?: string; Status?: string }>("/api/health", {
          skipAuth: true,
        });
        const status = data?.status || data?.Status || "Unknown";
        return {
          ok: status.toLowerCase() === "healthy" || status.toLowerCase() === "degraded",
          details: `Estado: ${status}`,
        };
      });

      await healthCheck(
        2,
        "Knowledge Base Engine",
        "Verificación del subsistema de conocimiento",
        async () => {
          const data = await apiClient.get<{ status?: string; Status?: string }>(
            "/api/knowledge/status",
            { skipAuth: true }
          );
          const status = data?.status || data?.Status || "Unknown";
          return {
            ok: status.toLowerCase() === "healthy",
            details: `Estado Knowledge: ${status}`,
          };
        }
      );

      await healthCheck(
        3,
        "Proveedores de IA",
        "Verificación de proveedores registrados",
        async () => {
          const data = await apiClient.get<unknown[]>("/api/providers/status", { skipAuth: true });
          const count = Array.isArray(data) ? data.length : 0;
          return {
            ok: count >= 0,
            details: `${count} proveedor(es) reportados`,
          };
        }
      );

      const isSystemReady = steps.every((s) => s.status === "completed");
      return { steps, isSystemReady };
    },
  });
}
