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

const MOCK_SECURITY_DATA: SecurityData = {
  roles: [
    {
      id: "r-admin",
      name: "Global Administrator",
      usersCount: 3,
      permissions: ["*"],
    },
    {
      id: "r-operator",
      name: "Agent Operator",
      usersCount: 12,
      permissions: ["agents.read", "agents.execute", "channels.read"],
    },
    {
      id: "r-developer",
      name: "API Developer",
      usersCount: 8,
      permissions: ["developer.keys", "webhooks.manage"],
    },
  ],
  vault: {
    algorithm: "AES-256-GCM (Tenant Isolated)",
    keyRotationDays: 45,
    totalSecretsEncrypted: 24,
    status: "healthy",
  },
};

export function useSecurityData() {
  return useQuery<SecurityData>({
    queryKey: ["securityData"],
    queryFn: async () => {
      await new Promise((r) => setTimeout(r, 350));
      return MOCK_SECURITY_DATA;
    },
    staleTime: 30000,
  });
}
