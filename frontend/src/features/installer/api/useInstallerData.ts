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

const MOCK_INSTALLER: InstallerData = {
  steps: [
    { id: 1, title: "Database PostgreSQL & Migraciones EF Core", description: "Verificación de esquema y tablas de OCAP", status: "completed", details: "Conexión a PostgreSQL verificada. 18 tablas activas." },
    { id: 2, title: "Credential Vault & Cifrado AES-256", description: "Inicialización de claves master del tenant", status: "completed", details: "Clave AES-256 derivada y lista para almacenar secretos." },
    { id: "3" as any, title: "Configuración de Proveedores IA", description: "OpenAI, Gemini, Ollama & Modelos Locales", status: "completed", details: "OpenAI y Ollama registrados en runtime." },
    { id: 4, title: "Registro de Adaptadores Omnicanal", description: "Telegram Bot, WhatsApp Business, Gmail", status: "completed", details: "Adaptador Telegram Native activo." },
  ],
  isSystemReady: true,
};

export function useInstallerData() {
  return useQuery<InstallerData>({
    queryKey: ["installerData"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_INSTALLER;
    },
    staleTime: 30000,
  });
}
