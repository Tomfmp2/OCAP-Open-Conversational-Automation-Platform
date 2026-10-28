"use client";

import { Select } from "@/shared/components/ui";
import type { TeamRole } from "../api/useTeam";

export const ROLE_LABELS: Record<TeamRole, { label: string; description: string }> = {
  Admin: { label: "Administrador", description: "Configura canales, IA, agentes, conocimiento y equipo." },
  Operator: { label: "Operador", description: "Atiende conversaciones y usa el asistente." },
};

export function RoleSelect({ value, onChange, disabled }: { value: TeamRole; onChange: (role: TeamRole) => void; disabled?: boolean }) {
  return (
    <Select label="Rol" value={value} disabled={disabled} hint={ROLE_LABELS[value].description} onChange={(e) => onChange(e.target.value as TeamRole)}>
      {(Object.keys(ROLE_LABELS) as TeamRole[]).map((role) => (
        <option key={role} value={role}>
          {ROLE_LABELS[role].label}
        </option>
      ))}
    </Select>
  );
}
