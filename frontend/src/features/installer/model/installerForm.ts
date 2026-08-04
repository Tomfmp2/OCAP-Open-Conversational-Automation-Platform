export type InstallerTarget = "Dev" | "Local" | "Web";

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
  allowsAnonymousSetup?: boolean;
}

export interface InstallerSetupResponse {
  success: boolean;
  requiresRestart: boolean;
  adminCreated: boolean;
  adminUpdated?: boolean;
  dotEnvWritten?: boolean;
  message: string;
  envKeysUpdated?: string[];
  envFilePath?: string;
  dotEnvPath?: string;
  restartHint: string;
  status: InstallerStatus;
}

export const defaultInstallerForm = (): InstallerFormState => ({
  target: "Dev",
  frontendHostPort: 3000,
  apiHostPort: 5229,
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
  tenantName: "OCAP Local",
  tenantSlug: "local",
  enableGoogleWorkspace: false,
  googleClientId: "",
  googleClientSecret: "",
  googleRedirectUri: "",
  aiProvider: "Gemini",
  aiApiKey: "",
  aiModelName: "gemini-3.5-flash",
  aiBaseUrl: "",
  enableWhatsApp: false,
  evolutionApiUrl: "http://localhost:8088",
  evolutionApiKey: "",
  enableTelegram: false,
  telegramBotToken: "",
});

const CORE_STEPS = [
  { id: "mode", title: "Modo" },
  { id: "admin", title: "Admin" },
  { id: "ai", title: "IA" },
  { id: "google", title: "Google" },
  { id: "review", title: "Revisar" },
  { id: "diagnostic", title: "Diagnóstico" },
] as const;

export type InstallerStepId =
  | "mode"
  | "network"
  | "database"
  | "admin"
  | "google"
  | "ai"
  | "review"
  | "diagnostic";

/** Pasos del wizard según target (v1 mínimo; canales se configuran en la app). */
export function getInstallerSteps(target: InstallerTarget): { id: InstallerStepId; title: string }[] {
  if (target === "Web") {
    return [
      { id: "mode", title: "Modo" },
      { id: "network", title: "Red" },
      { id: "database", title: "PostgreSQL" },
      { id: "admin", title: "Admin" },
      { id: "ai", title: "IA" },
      { id: "google", title: "Google" },
      { id: "review", title: "Revisar" },
      { id: "diagnostic", title: "Diagnóstico" },
    ];
  }
  if (target === "Local") {
    return [
      { id: "mode", title: "Modo" },
      { id: "network", title: "Red" },
      { id: "admin", title: "Admin" },
      { id: "ai", title: "IA" },
      { id: "google", title: "Google" },
      { id: "review", title: "Revisar" },
      { id: "diagnostic", title: "Diagnóstico" },
    ];
  }
  return [...CORE_STEPS];
}

/** @deprecated Usar getInstallerSteps(target) */
export const INSTALLER_STEPS = getInstallerSteps("Dev");

export function validateInstallerStep(
  step: InstallerStepId,
  form: InstallerFormState
): string | null {
  switch (step) {
    case "network":
      if (form.target === "Dev" || form.target === "Local") return null;
      if (!/^https?:\/\//i.test(form.publicApiUrl))
        return "URL pública de la API requerida (http/https).";
      if (!/^https?:\/\//i.test(form.publicPanelUrl))
        return "URL pública del panel requerida (http/https).";
      return null;
    case "database":
      if (form.target !== "Web") return null;
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
      // Dev: key opcional (Mock / .env existente). Local/Web: obligatoria salvo Ollama.
      if (
        form.target !== "Dev" &&
        form.aiProvider !== "Ollama" &&
        !form.aiApiKey.trim()
      )
        return "API key de IA requerida (salvo Ollama).";
      return null;
    default:
      return null;
  }
}

export function resolveApiUrl(form: InstallerFormState): string {
  if (form.target === "Web") return form.publicApiUrl.replace(/\/$/, "");
  if (form.target === "Local") return "http://localhost:5000";
  return "http://localhost:5229";
}

/**
 * Extrae client_id + client_secret del JSON que descarga Google Cloud Console
 * (OAuth client → Descargar JSON). No existe API pública ClientId→Secret.
 */
export function parseGoogleCredentialsJson(raw: string): {
  clientId: string;
  clientSecret: string;
  redirectUri?: string;
} | null {
  const trimmed = raw.trim();
  if (!trimmed) return null;

  try {
    const parsed = JSON.parse(trimmed) as Record<string, unknown>;
    const block =
      (parsed.web as Record<string, unknown> | undefined) ??
      (parsed.installed as Record<string, unknown> | undefined) ??
      parsed;

    const clientId = String(block.client_id ?? block.clientId ?? "").trim();
    const clientSecret = String(block.client_secret ?? block.clientSecret ?? "").trim();
    if (!clientId || !clientSecret) return null;

    const redirects = block.redirect_uris ?? block.redirectUris;
    let redirectUri: string | undefined;
    if (Array.isArray(redirects) && typeof redirects[0] === "string") {
      redirectUri = redirects[0];
    } else if (typeof redirects === "string") {
      redirectUri = redirects;
    }

    return { clientId, clientSecret, redirectUri };
  } catch {
    return null;
  }
}

export function toSetupPayload(form: InstallerFormState) {
  const apiUrl = resolveApiUrl(form);
  const panelUrl =
    form.target === "Web"
      ? form.publicPanelUrl.replace(/\/$/, "")
      : "http://localhost:3000";
  const redirect =
    form.googleRedirectUri.trim() ||
    `${apiUrl}/api/integrations/Google/connect`;

  return {
    target: form.target,
    frontendHostPort: form.target === "Web" ? form.frontendHostPort : 3000,
    apiHostPort:
      form.target === "Dev" ? 5229 : form.target === "Local" ? 5000 : form.apiHostPort,
    publicApiUrl: apiUrl,
    publicPanelUrl: panelUrl,
    postgresHost: form.postgresHost,
    postgresPort: form.postgresPort,
    postgresDbName: form.postgresDbName,
    postgresUsername: form.postgresUsername,
    postgresPassword:
      form.target === "Local" ? "OcapSecurePass2026!" : form.postgresPassword,
    adminEmail: form.adminEmail,
    adminPassword: form.adminPassword,
    tenantName: form.tenantName,
    tenantSlug: form.tenantSlug,
    enableGoogleWorkspace: form.enableGoogleWorkspace,
    googleClientId: form.googleClientId,
    googleClientSecret: form.googleClientSecret,
    googleRedirectUri: redirect,
    aiProvider: form.aiProvider,
    aiApiKey: form.aiApiKey || null,
    aiModelName: form.aiModelName,
    aiBaseUrl: form.aiBaseUrl || null,
    enableWhatsApp: false,
    evolutionApiUrl: form.evolutionApiUrl,
    evolutionApiKey: form.evolutionApiKey,
    enableTelegram: false,
    telegramBotToken: form.telegramBotToken,
  };
}
