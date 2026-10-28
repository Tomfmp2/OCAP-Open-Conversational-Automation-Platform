import type { StoredUser } from "./types";

/**
 * La sesión vive solo en la memoria de la pestaña. El refresh token está en una cookie HttpOnly que JavaScript no
 * puede leer; al recargar, AuthProvider la canjea por un access token nuevo. Nada de la sesión toca localStorage,
 * así un XSS no puede llevarse credenciales que sobrevivan a la pestaña.
 */
let accessToken: string | null = null;
let user: StoredUser | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function getStoredUser(): StoredUser | null {
  return user;
}

export function getTenantId(): string | null {
  return user?.tenantId ?? null;
}

export function setAuthSession(token: string, sessionUser: StoredUser): void {
  accessToken = token;
  user = sessionUser;
}

export function updateAccessToken(token: string): void {
  accessToken = token;
}

export function clearAuthSession(): void {
  accessToken = null;
  user = null;
}
