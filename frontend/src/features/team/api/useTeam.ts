import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";

export type TeamRole = "Admin" | "Operator";

export interface TeamMember {
  id: string;
  email: string;
  fullName: string;
  role: TeamRole;
  isActive: boolean;
  isLocked: boolean;
  invitationPending: boolean;
  mfaEnabled: boolean;
  createdAtUtc: string;
}

export interface PasswordLink {
  /** Ruta relativa del panel (/accept?token=…): el enlace se arma con el origen desde el que se usa el panel. */
  path: string;
  expiresAtUtc: string;
  emailed: boolean;
}

const TEAM_KEY = ["team"];

export function useTeam() {
  return useQuery({
    queryKey: TEAM_KEY,
    queryFn: () => apiClient.get<{ members: TeamMember[]; emailConfigured: boolean }>("/api/team"),
  });
}

export function useTeamActions() {
  const queryClient = useQueryClient();
  const refresh = () => queryClient.invalidateQueries({ queryKey: TEAM_KEY });

  return {
    invite: useMutation({
      mutationFn: (payload: { email: string; fullName: string; role: TeamRole }) =>
        apiClient.post<{ member: TeamMember; link: PasswordLink }>("/api/team/invitations", payload, { skipRetry: true }),
      onSuccess: refresh,
    }),
    passwordLink: useMutation({
      mutationFn: (id: string) => apiClient.post<PasswordLink>(`/api/team/${id}/password-link`, {}),
      onSuccess: refresh,
    }),
    changeRole: useMutation({
      mutationFn: ({ id, role }: { id: string; role: TeamRole }) => apiClient.put(`/api/team/${id}/role`, { role }),
      onSuccess: refresh,
    }),
    setActive: useMutation({
      mutationFn: ({ id, active }: { id: string; active: boolean }) =>
        apiClient.post(`/api/team/${id}/${active ? "activate" : "deactivate"}`, {}),
      onSuccess: refresh,
    }),
    unlock: useMutation({
      mutationFn: (id: string) => apiClient.post(`/api/team/${id}/unlock`, {}),
      onSuccess: refresh,
    }),
  };
}

export function absoluteLink(link: PasswordLink): string {
  return typeof window === "undefined" ? link.path : `${window.location.origin}${link.path}`;
}
