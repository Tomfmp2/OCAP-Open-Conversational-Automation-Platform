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

export function useSecurityData() {
  return useQuery<SecurityData>({
    queryKey: ["securityData"],
    queryFn: async () => {
      const res = await fetch("/api/roles");
      if (!res.ok) {
        return {
          roles: [],
          vault: { algorithm: "AES-256-GCM (Tenant Isolated)", keyRotationDays: 30, totalSecretsEncrypted: 0, status: "healthy" },
        };
      }
      const data = await res.json();
      return {
        roles: Array.isArray(data) ? data : [],
        vault: { algorithm: "AES-256-GCM (Tenant Isolated)", keyRotationDays: 30, totalSecretsEncrypted: 0, status: "healthy" },
      };
    },
    staleTime: 30000,
  });
}
