import type { AuthTokens, StoredUser } from "./types";

const KEYS = {
  accessToken: "ocap.accessToken",
  refreshToken: "ocap.refreshToken",
  tenantId: "ocap.tenantId",
  user: "ocap.user",
} as const;

let memoryAccessToken: string | null = null;
let memoryRefreshToken: string | null = null;
let memoryTenantId: string | null = null;
let memoryUser: StoredUser | null = null;

function isBrowser(): boolean {
  return typeof window !== "undefined";
}

export function getAccessToken(): string | null {
  if (memoryAccessToken) return memoryAccessToken;
  if (!isBrowser()) return null;
  return localStorage.getItem(KEYS.accessToken);
}

export function getRefreshToken(): string | null {
  if (memoryRefreshToken) return memoryRefreshToken;
  if (!isBrowser()) return null;
  return localStorage.getItem(KEYS.refreshToken);
}

export function getTenantId(): string | null {
  if (memoryTenantId) return memoryTenantId;
  if (!isBrowser()) return null;
  return localStorage.getItem(KEYS.tenantId);
}

export function getStoredUser(): StoredUser | null {
  if (memoryUser) return memoryUser;
  if (!isBrowser()) return null;
  const raw = localStorage.getItem(KEYS.user);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as StoredUser;
  } catch {
    return null;
  }
}

export function setAuthSession(tokens: AuthTokens, user: StoredUser): void {
  memoryAccessToken = tokens.accessToken;
  memoryRefreshToken = tokens.refreshToken;
  memoryTenantId = tokens.tenantId;
  memoryUser = user;

  if (!isBrowser()) return;
  localStorage.setItem(KEYS.accessToken, tokens.accessToken);
  localStorage.setItem(KEYS.refreshToken, tokens.refreshToken);
  localStorage.setItem(KEYS.tenantId, tokens.tenantId);
  localStorage.setItem(KEYS.user, JSON.stringify(user));
}

export function updateAccessToken(accessToken: string): void {
  memoryAccessToken = accessToken;
  if (isBrowser()) {
    localStorage.setItem(KEYS.accessToken, accessToken);
  }
}

export function clearAuthSession(): void {
  memoryAccessToken = null;
  memoryRefreshToken = null;
  memoryTenantId = null;
  memoryUser = null;

  if (!isBrowser()) return;
  localStorage.removeItem(KEYS.accessToken);
  localStorage.removeItem(KEYS.refreshToken);
  localStorage.removeItem(KEYS.tenantId);
  localStorage.removeItem(KEYS.user);
}
