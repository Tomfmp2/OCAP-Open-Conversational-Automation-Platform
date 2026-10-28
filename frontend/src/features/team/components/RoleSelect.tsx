"use client";

import type { TeamRole } from "../api/useTeam";

export const ROLE_LABELS: Record<TeamRole, { label: string; description: string }> = {
  Admin: { label: "Administrador", description: "Todo: canales, IA, agentes, conocimiento y equipo." },
  Operator: { label: "Operador", description: "Atiende la bandeja y usa el asistente." },
};

export function RoleSelect({
  id,
  value,
  onChange,
  disabled,
}: {
  id: string;
  value: TeamRole;
  onChange: (role: TeamRole) => void;
  disabled?: boolean;
}) {
  return (
    <div className="space-y-1">
      <label htmlFor={id} className="block text-xs font-medium text-neutral-700">
        Rol
      </label>
      <select
        id={id}
        value={value}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value as TeamRole)}
        aria-describedby={`${id}-help`}
        className="focus-ring w-full rounded-md border border-neutral-300 bg-white px-2.5 py-2 text-sm text-neutral-900 disabled:opacity-60"
      >
        {(Object.keys(ROLE_LABELS) as TeamRole[]).map((role) => (
          <option key={role} value={role}>
            {ROLE_LABELS[role].label}
          </option>
        ))}
      </select>
      <p id={`${id}-help`} className="text-xs text-neutral-500">
        {ROLE_LABELS[value].description}
      </p>
    </div>
  );
}
