import { useQuery } from "@tanstack/react-query";

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

export interface SecurityData {
  roles: RbacRole[];
  vault: VaultStatus;
}

const DEFAULT_SECURITY_DATA: SecurityData = {
  roles: [],
  vault: { algorithm: "AES-256-GCM (Tenant Isolated)", keyRotationDays: 30, totalSecretsEncrypted: 0, status: "healthy" },
};

export function useSecurityData() {
  return useQuery<SecurityData>({
    queryKey: ["securityData"],
    queryFn: async () => {
      try {
        if (typeof window === "undefined") return DEFAULT_SECURITY_DATA;
        const res = await fetch("/api/roles");
        if (!res.ok) return DEFAULT_SECURITY_DATA;
        const data = await res.json();
        return {
          roles: Array.isArray(data) ? data : [],
          vault: DEFAULT_SECURITY_DATA.vault,
        };
      } catch {
        return DEFAULT_SECURITY_DATA;
      }
    },
    staleTime: 30000,
  });
}
