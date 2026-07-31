export type InstallerTarget = "Local" | "Web";

export interface InstallerFormState {
  target: InstallerTarget;
  frontendHostPort: number;
  apiHostPort: number;
  publicApiUrl: string;
  publicPanelUrl: string;
  postgresHost: string;
  postgresPort: number;
  postgresDbName: string;
  postgresUsername: string;
  postgresPassword: string;
  adminEmail: string;
  adminPassword: string;
  adminPasswordConfirm: string;
  tenantName: string;
  tenantSlug: string;
  enableGoogleWorkspace: boolean;
  googleClientId: string;
  googleClientSecret: string;
  googleRedirectUri: string;
  aiProvider: "OpenAI" | "Gemini" | "Claude" | "Ollama";
  aiApiKey: string;
  aiModelName: string;
  aiBaseUrl: string;
  enableWhatsApp: boolean;
  evolutionApiUrl: string;
  evolutionApiKey: string;
  enableTelegram: boolean;
  telegramBotToken: string;
}

export interface InstallerStatus {
  completed: boolean;
  target: string;
  frontendHostPort?: number | null;
  apiHostPort?: number | null;
  publicApiUrl?: string | null;
  publicPanelUrl?: string | null;
  hasAdminUsers: boolean;
  googleConfigured: boolean;
  aiConfigured: boolean;
  configPath: string;
}

export interface InstallerSetupResponse {
  success: boolean;
  requiresRestart: boolean;
  adminCreated: boolean;
  adminUpdated?: boolean;
  dotEnvWritten?: boolean;
  message: string;
  envFilePreview: string;
  envFilePath?: string;
  dotEnvPath?: string;
  restartHint: string;
  status: InstallerStatus;
}

export const defaultInstallerForm = (): InstallerFormState => ({
  target: "Local",
  frontendHostPort: 3000,
  apiHostPort: 5000,
  publicApiUrl: "https://api.example.com",
  publicPanelUrl: "https://app.example.com",
  postgresHost: "localhost",
  postgresPort: 5433,
  postgresDbName: "ocap_db",
  postgresUsername: "ocap_user",
  postgresPassword: "OcapSecurePass2026!",
  adminEmail: "",
  adminPassword: "",
  adminPasswordConfirm: "",
  tenantName: "OCAP Default",
  tenantSlug: "default",
  enableGoogleWorkspace: true,
  googleClientId: "",
  googleClientSecret: "",
  googleRedirectUri: "",
  aiProvider: "OpenAI",
  aiApiKey: "",
  aiModelName: "gpt-4o",
  aiBaseUrl: "",
  enableWhatsApp: false,
  evolutionApiUrl: "http://localhost:8088",
  evolutionApiKey: "",
  enableTelegram: false,
  telegramBotToken: "",
});

export const INSTALLER_STEPS = [
  { id: "mode", title: "Modo" },
  { id: "network", title: "Red" },
  { id: "database", title: "PostgreSQL" },
  { id: "admin", title: "Admin" },
  { id: "google", title: "Google" },
  { id: "ai", title: "IA" },
  { id: "channels", title: "Canales" },
  { id: "review", title: "Revisar" },
  { id: "diagnostic", title: "Diagnóstico" },
] as const;

export type InstallerStepId = (typeof INSTALLER_STEPS)[number]["id"];

export function validateInstallerStep(
  step: InstallerStepId,
  form: InstallerFormState
): string | null {
  switch (step) {
    case "network":
      if (form.target === "Local") {
        // Local Docker: puertos fijos 3000/5000 — el stack ya los publica.
        return null;
      }
      if (!/^https?:\/\//i.test(form.publicApiUrl))
        return "URL pública de la API requerida (http/https).";
      if (!/^https?:\/\//i.test(form.publicPanelUrl))
        return "URL pública del panel requerida (http/https).";
      return null;
    case "database":
      if (form.target === "Local") {
        // Compose aporta Postgres; no se cambia el password del volumen desde el wizard.
        return null;
      }
      if (!form.postgresHost.trim()) return "Host de PostgreSQL requerido.";
      if (!form.postgresDbName.trim()) return "Nombre de base requerido.";
      if (!form.postgresUsername.trim()) return "Usuario de PostgreSQL requerido.";
      if (form.postgresPassword.length < 8)
        return "Contraseña de PostgreSQL: mínimo 8 caracteres.";
      return null;
    case "admin":
      if (!form.adminEmail.includes("@")) return "Email admin inválido.";
      if (form.adminPassword.length < 10)
        return "Contraseña admin: mínimo 10 caracteres.";
      if (form.adminPassword !== form.adminPasswordConfirm)
        return "Las contraseñas admin no coinciden.";
      if (!form.tenantName.trim()) return "Nombre de organización requerido.";
      if (!/^[a-z0-9-]+$/.test(form.tenantSlug))
        return "Slug: solo minúsculas, números y guiones.";
      return null;
    case "google":
      if (form.enableGoogleWorkspace) {
        if (!form.googleClientId.trim()) return "Google Client ID requerido.";
        if (!form.googleClientSecret.trim())
          return "Google Client Secret requerido.";
      }
      return null;
    case "ai":
      if (!form.aiModelName.trim()) return "Modelo de IA requerido.";
      if (form.aiProvider !== "Ollama" && !form.aiApiKey.trim())
        return "API key de IA requerida (salvo Ollama).";
      return null;
    case "channels":
      if (form.enableWhatsApp) {
        if (!form.evolutionApiUrl.trim()) return "URL de Evolution API requerida.";
        if (!form.evolutionApiKey.trim()) return "API key de Evolution requerida.";
      }
      if (form.enableTelegram && !form.telegramBotToken.trim())
        return "Bot token de Telegram requerido.";
      return null;
    default:
      return null;
  }
}

export function toSetupPayload(form: InstallerFormState) {
  const isLocal = form.target === "Local";
  const apiUrl = isLocal
    ? "http://localhost:5000"
    : form.publicApiUrl.replace(/\/$/, "");
  const redirect =
    form.googleRedirectUri.trim() ||
    `${apiUrl}/api/integrations/Google/connect`;

  return {
    target: form.target,
    frontendHostPort: isLocal ? 3000 : form.frontendHostPort,
    apiHostPort: isLocal ? 5000 : form.apiHostPort,
    publicApiUrl: isLocal ? "http://localhost:5000" : form.publicApiUrl,
    publicPanelUrl: isLocal ? "http://localhost:3000" : form.publicPanelUrl,
    postgresHost: form.postgresHost,
    postgresPort: form.postgresPort,
    postgresDbName: form.postgresDbName,
    postgresUsername: form.postgresUsername,
    postgresPassword: isLocal ? "OcapSecurePass2026!" : form.postgresPassword,
    adminEmail: form.adminEmail,
    adminPassword: form.adminPassword,
    tenantName: form.tenantName,
    tenantSlug: form.tenantSlug,
    enableGoogleWorkspace: form.enableGoogleWorkspace,
    googleClientId: form.googleClientId,
    googleClientSecret: form.googleClientSecret,
    googleRedirectUri: redirect,
    aiProvider: form.aiProvider,
    aiApiKey: form.aiApiKey,
    aiModelName: form.aiModelName,
    aiBaseUrl: form.aiBaseUrl || null,
    enableWhatsApp: form.enableWhatsApp,
    evolutionApiUrl: form.evolutionApiUrl,
    evolutionApiKey: form.evolutionApiKey,
    enableTelegram: form.enableTelegram,
    telegramBotToken: form.telegramBotToken,
  };
}
