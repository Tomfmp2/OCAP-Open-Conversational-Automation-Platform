"use client";

import React from "react";
import {
 authApi,
 mapLoginToStoredUser,
 type LoginRequest,
 type LoginResponse,
} from "@/features/auth/api/authApi";
import {
 clearAuthSession,
 getAccessToken,
 getRefreshToken,
 getStoredUser,
 setAuthSession,
 updateAccessToken,
} from "@/shared/api/tokenStorage";
import type { StoredUser } from "@/shared/api/types";
import { apiClient, defaultRefreshHandler } from "@/shared/api/apiClient";
import { useTenantStore } from "@/shared/stores/useTenantStore";
import { redirectToLogin } from "@/shared/api/sessionRedirect";
import { permissionsFromToken } from "@/shared/api/jwt";

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


 const bootstrapSession = React.useCallback(async () => {
 const token = getAccessToken();
 const storedUser = getStoredUser();

 if (!token || !storedUser) {
 setUser(null);
 setIsLoading(false);
 return;
 }

 try {
 const profile = await authApi.getProfile();
 // Los permisos salen del JWT vigente: /api/roles solo lo puede leer quien administra la seguridad.
 const permissions = permissionsFromToken(token);

 const hydrated = mapLoginToStoredUser(
 {
 accessToken: token,
 refreshToken: "",
 userId: profile.id,
 tenantId: profile.tenantId,
 email: profile.email,
 roleName: storedUser.roleName,
 },
 profile,
 permissions
 );

 setUser(hydrated);
 syncTenantFromAuth(hydrated.tenantId, hydrated.fullName);
 } catch {
 clearAuthSession();
 setUser(null);
 } finally {
 setIsLoading(false);
 }
 }, [syncTenantFromAuth]);

 React.useEffect(() => {
 apiClient.setRefreshHandler(async () => {
 const stored = getStoredUser();
 const refreshToken = localStorage.getItem("ocap.refreshToken");
 if (!refreshToken) return null;

 try {
 const result = await authApi.refresh(refreshToken);
 updateAccessToken(result.accessToken);
 if (stored) {
 setAuthSession(
 {
 accessToken: result.accessToken,
 refreshToken: result.refreshToken,
 tenantId: result.tenantId,
 },
 {
 ...stored,
 tenantId: result.tenantId,
 roleName: result.roleName,
 permissions: permissionsFromToken(result.accessToken),
 }
 );
 }
 return result.accessToken;
 } catch {
 return null;
 }
 });

 let cancelled = false;
 // Bootstrap de sesión al montar: patrón estándar de hidratación auth
 // eslint-disable-next-line react-hooks/set-state-in-effect -- setState ocurre en callbacks async tras fetch
 void bootstrapSession().then(() => {
 if (cancelled) return;
 });

 return () => {
 cancelled = true;
 };
 }, [bootstrapSession]);

 React.useEffect(() => {
 if (!user) return;

 const interval = setInterval(async () => {
 const token = await defaultRefreshHandler();
 if (!token) {
 clearAuthSession();
 setUser(null);
 redirectToLogin();
 }
 }, REFRESH_INTERVAL_MS);

 return () => clearInterval(interval);
 }, [user]);

 const adoptSession = React.useCallback(
 async (result: LoginResponse) => {
 const provisionalUser = mapLoginToStoredUser(result);
 setAuthSession(
 {
 accessToken: result.accessToken,
 refreshToken: result.refreshToken,
 tenantId: result.tenantId,
 },
 provisionalUser
 );

 const permissions = permissionsFromToken(result.accessToken);
 const profile = await authApi.getProfile().catch(() => undefined);
 const storedUser = mapLoginToStoredUser(result, profile, permissions);

 setAuthSession(
 {
 accessToken: result.accessToken,
 refreshToken: result.refreshToken,
 tenantId: result.tenantId,
 },
 storedUser
 );

 setUser(storedUser);
 syncTenantFromAuth(result.tenantId, profile?.fullName || result.email);
 },
 [syncTenantFromAuth]
 );

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
 const refreshToken = getRefreshToken();
 try {
 await authApi.logout(refreshToken);
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
