import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export interface RbacRole {
  id: string;
  name: string;
  usersCount: number;
  permissions: string[];
}

export interface VaultStatus {
  algorithm: string;
  keyRotationDays: number;
  totalSecretsEncrypted: number;
  status: "healthy" | "rotation_due";
}

export interface SecurityUser {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
}

export interface SecuritySession {
  id: string;
  userId: string;
  ipAddress: string;
  loginAtUtc: string;
  isActive: boolean;
}

export interface SecurityTenant {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
}

export interface SecurityPermission {
  id: string;
  code: string;
  name: string;
  category: string;
}

export interface SecurityData {
  roles: RbacRole[];
  vault: VaultStatus;
  users: SecurityUser[];
  sessions: SecuritySession[];
  tenants: SecurityTenant[];
  permissions: SecurityPermission[];
  apiKeyCount: number;
}

export function useSecurityData() {
  return useQuery<SecurityData>({
    queryKey: ["securityData"],
    queryFn: async () => {
      const [roles, apiKeys, users, sessions, tenants, permissions] =
        await Promise.allSettled([
          apiClient.get<Array<{ id: string; name: string; permissions: string[] }>>("/api/roles"),
          apiClient.get<unknown[] | { apiKeys?: unknown[] }>("/api/apikeys"),
          apiClient.get<
            Array<{ id: string; email: string; fullName: string; isActive: boolean }>
          >("/api/users"),
          apiClient.get<
            Array<{
              id: string;
              userId: string;
              ipAddress: string;
              loginAtUtc: string;
              isActive: boolean;
            }>
          >("/api/sessions"),
          apiClient.get<
            Array<{ id: string; name: string; slug: string; isActive: boolean }>
          >("/api/tenants"),
          apiClient.get<
            Array<{ id: string; code: string; name: string; category: string }>
          >("/api/permissions"),
        ]);

      const rolesList =
        roles.status === "fulfilled" && Array.isArray(roles.value) ? roles.value : [];

      let keyCount = 0;
      if (apiKeys.status === "fulfilled") {
        const raw = apiKeys.value;
        const list = Array.isArray(raw) ? raw : raw?.apiKeys || [];
        keyCount = list.length;
      }

      const usersList =
        users.status === "fulfilled" && Array.isArray(users.value) ? users.value : [];
      const sessionsList =
        sessions.status === "fulfilled" && Array.isArray(sessions.value)
          ? sessions.value
          : [];
      const tenantsList =
        tenants.status === "fulfilled" && Array.isArray(tenants.value)
          ? tenants.value
          : [];
      const permissionsList =
        permissions.status === "fulfilled" && Array.isArray(permissions.value)
          ? permissions.value
          : [];

      return {
        roles: rolesList.map((r) => ({
          id: r.id,
          name: r.name,
          usersCount: 0,
          permissions: r.permissions || [],
        })),
        vault: {
          algorithm: "AES-256-GCM (Tenant Isolated)",
          keyRotationDays: 30,
          totalSecretsEncrypted: keyCount,
          status: "healthy" as const,
        },
        users: usersList.map((u) => ({
          id: u.id,
          email: u.email,
          fullName: u.fullName,
          isActive: u.isActive,
        })),
        sessions: sessionsList.map((s) => ({
          id: s.id,
          userId: s.userId,
          ipAddress: s.ipAddress,
          loginAtUtc: s.loginAtUtc,
          isActive: s.isActive,
        })),
        tenants: tenantsList.map((t) => ({
          id: t.id,
          name: t.name,
          slug: t.slug,
          isActive: t.isActive,
        })),
        permissions: permissionsList.map((p) => ({
          id: p.id,
          code: p.code,
          name: p.name,
          category: p.category,
        })),
        apiKeyCount: keyCount,
      };
    },
    staleTime: 30000,
    retry: 2,
  });
}
