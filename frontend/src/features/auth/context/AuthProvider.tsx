"use client";

import React from "react";
import {
 authApi,
 mapLoginToStoredUser,
 type LoginRequest,
 type LoginResponse,
} from "@/features/auth/api/authApi";
import { clearAuthSession, setAuthSession } from "@/shared/api/tokenStorage";
import type { StoredUser } from "@/shared/api/types";
import { apiClient, refreshSession } from "@/shared/api/apiClient";
import { useTenantStore } from "@/shared/stores/useTenantStore";
import { redirectToLogin } from "@/shared/api/sessionRedirect";
import { permissionsFromToken } from "@/shared/api/jwt";

// El access token dura 15 minutos: se renueva un poco antes con la cookie del refresh token.
const REFRESH_INTERVAL_MS = 14 * 60 * 1000;
// Permiso de plataforma que cumple todos los demás (igual que en la API).
const FULL_ACCESS = "Admin.Full";

interface AuthContextValue {
 user: StoredUser | null;
 isAuthenticated: boolean;
 isLoading: boolean;
 /** Devuelve el desafío si la cuenta tiene MFA; la sesión se abre con completeMfa. */
 login: (credentials: LoginRequest) => Promise<{ mfaToken: string } | null>;
 completeMfa: (mfaToken: string, code: string) => Promise<void>;
 /** Abre la sesión con tokens ya emitidos (p. ej. al terminar la configuración inicial). */
 adoptSession: (result: LoginResponse) => Promise<void>;
 logout: () => Promise<void>;
 hasRole: (role: string) => boolean;
 hasPermission: (permission: string) => boolean;
}

const AuthContext = React.createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
 const [user, setUser] = React.useState<StoredUser | null>(null);
 const [isLoading, setIsLoading] = React.useState(true);
 const syncTenantFromAuth = useTenantStore((s) => s.syncFromAuth);

 const adoptSession = React.useCallback(
 async (result: LoginResponse) => {
 // El perfil necesita el access token: primero una sesión provisional, luego la completa.
 setAuthSession(result.accessToken, mapLoginToStoredUser(result));
 const permissions = permissionsFromToken(result.accessToken);
 const profile = await authApi.getProfile().catch(() => undefined);
 const sessionUser = mapLoginToStoredUser(result, profile, permissions);

 setAuthSession(result.accessToken, sessionUser);
 setUser(sessionUser);
 syncTenantFromAuth(result.tenantId, profile?.fullName || result.email);
 },
 [syncTenantFromAuth]
 );

 React.useEffect(() => {
 // Tras un 401 la API pide un access token nuevo; si la cookie ya no sirve, la sesión terminó.
 apiClient.setRefreshHandler(async () => (await refreshSession())?.accessToken ?? null);

 let cancelled = false;
 // Al cargar la página no hay nada en memoria: la cookie HttpOnly, si existe, recupera la sesión.
 void (async () => {
 const session = await refreshSession();
 try {
 if (session && !cancelled) await adoptSession(session);
 } catch {
 clearAuthSession();
 } finally {
 if (!cancelled) setIsLoading(false);
 }
 })();

 return () => {
 cancelled = true;
 };
 }, [adoptSession]);

 React.useEffect(() => {
 if (!user) return;

 const interval = setInterval(async () => {
 if (!(await refreshSession())) {
 clearAuthSession();
 setUser(null);
 redirectToLogin();
 }
 }, REFRESH_INTERVAL_MS);

 return () => clearInterval(interval);
 }, [user]);

 const login = React.useCallback(
 async (credentials: LoginRequest) => {
 const result = await authApi.login(credentials);
 if (result.mfaRequired && result.mfaToken) return { mfaToken: result.mfaToken };
 await adoptSession(result);
 return null;
 },
 [adoptSession]
 );

 const completeMfa = React.useCallback(
 async (mfaToken: string, code: string) => {
 await adoptSession(await authApi.verifyMfa(mfaToken, code));
 },
 [adoptSession]
 );

 const logout = React.useCallback(async () => {
 try {
 await authApi.logout();
 } catch {
 // ignore logout errors
 } finally {
 clearAuthSession();
 setUser(null);
 redirectToLogin();
 }
 }, []);

 const hasRole = React.useCallback(
 (role: string) => user?.roleName.toLowerCase() === role.toLowerCase(),
 [user]
 );

 const hasPermission = React.useCallback(
 (permission: string) => {
 if (!user) return false;
 return user.permissions.includes(permission) || user.permissions.includes(FULL_ACCESS);
 },
 [user]
 );

 const value: AuthContextValue = {
 user,
 isAuthenticated: !!user,
 isLoading,
 login,
 completeMfa,
 adoptSession,
 logout,
 hasRole,
 hasPermission,
 };

 return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
 const ctx = React.useContext(AuthContext);
 if (!ctx) {
 throw new Error("useAuth debe usarse dentro de AuthProvider");
 }
 return ctx;
}
