"use client";

import React from "react";
import {
  authApi,
  mapLoginToStoredUser,
  type LoginRequest,
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

const REFRESH_INTERVAL_MS = 14 * 60 * 1000;

interface AuthContextValue {
  user: StoredUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
  hasPermission: (permission: string) => boolean;
}

const AuthContext = React.createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = React.useState<StoredUser | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);
  const syncTenantFromAuth = useTenantStore((s) => s.syncFromAuth);

  const loadPermissionsForRole = React.useCallback(async (roleName: string) => {
    try {
      const roles = await authApi.getRoles();
      const match = roles.find((r) => r.name.toLowerCase() === roleName.toLowerCase());
      return match?.permissions ?? [];
    } catch {
      return [];
    }
  }, []);

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
      const permissions =
        storedUser.permissions.length > 0
          ? storedUser.permissions
          : await loadPermissionsForRole(storedUser.roleName);

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
  }, [loadPermissionsForRole, syncTenantFromAuth]);

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
        if (typeof window !== "undefined") {
          window.location.href = "/login";
        }
      }
    }, REFRESH_INTERVAL_MS);

    return () => clearInterval(interval);
  }, [user]);

  const login = React.useCallback(
    async (credentials: LoginRequest) => {
      const result = await authApi.login(credentials);

      const provisionalUser = mapLoginToStoredUser(result);
      setAuthSession(
        {
          accessToken: result.accessToken,
          refreshToken: result.refreshToken,
          tenantId: result.tenantId,
        },
        provisionalUser
      );

      const permissions = await loadPermissionsForRole(result.roleName);
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
    [loadPermissionsForRole, syncTenantFromAuth]
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
      if (typeof window !== "undefined") {
        window.location.href = "/login";
      }
    }
  }, []);

  const hasRole = React.useCallback(
    (role: string) => user?.roleName.toLowerCase() === role.toLowerCase(),
    [user]
  );

  const hasPermission = React.useCallback(
    (permission: string) => {
      if (!user) return false;
      if (user.roleName.toLowerCase() === "admin") return true;
      return user.permissions.includes(permission);
    },
    [user]
  );

  const value: AuthContextValue = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
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
