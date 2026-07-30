import { apiClient } from "@/shared/api/apiClient";
import type { StoredUser } from "@/shared/api/types";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  tenantId: string;
  email: string;
  roleName: string;
}

export interface ProfileResponse {
  id: string;
  tenantId: string;
  email: string;
  fullName: string;
  isActive: boolean;
  isLocked: boolean;
  isEmailVerified: boolean;
  createdAtUtc: string;
}

export interface RoleDto {
  id: string;
  tenantId: string;
  name: string;
  description: string;
  permissions: string[];
}

export const authApi = {
  login(payload: LoginRequest): Promise<LoginResponse> {
    return apiClient.post<LoginResponse>("/api/auth/login", payload, { skipAuth: true });
  },

  refresh(refreshToken: string): Promise<LoginResponse> {
    return apiClient.post<LoginResponse>(
      "/api/auth/refresh",
      { refreshToken },
      { skipAuth: true }
    );
  },

  logout(): Promise<{ message: string }> {
    return apiClient.post<{ message: string }>("/api/auth/logout");
  },

  getProfile(): Promise<ProfileResponse> {
    return apiClient.get<ProfileResponse>("/api/profile");
  },

  getRoles(): Promise<RoleDto[]> {
    return apiClient.get<RoleDto[]>("/api/roles");
  },
};

export function mapLoginToStoredUser(
  login: LoginResponse,
  profile?: ProfileResponse,
  permissions: string[] = []
): StoredUser {
  return {
    id: login.userId,
    email: login.email,
    fullName: profile?.fullName || login.email,
    tenantId: login.tenantId,
    roleName: login.roleName,
    permissions,
  };
}
