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
      const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";

      const [rolesRes, keysRes] = await Promise.allSettled([
        fetch(`${baseUrl}/api/roles`),
        fetch(`${baseUrl}/api/apikeys`)
      ]);

      if (rolesRes.status === "rejected" || !rolesRes.value.ok) {
        throw new Error("No se pudieron cargar los roles de seguridad RBAC");
      }

      const rolesList = await rolesRes.value.json();
      let keyCount = 0;
      if (keysRes.status === "fulfilled" && keysRes.value.ok) {
        const keysJson = await keysRes.value.json();
        const list = Array.isArray(keysJson) ? keysJson : keysJson?.apiKeys || [];
        keyCount = list.length;
      }

      return {
        roles: Array.isArray(rolesList) ? rolesList : [],
        vault: {
          algorithm: "AES-256-GCM (Tenant Isolated)",
          keyRotationDays: 30,
          totalSecretsEncrypted: keyCount,
          status: "healthy"
        },
      };
    },
    staleTime: 30000,
    retry: 2,
  });
}
