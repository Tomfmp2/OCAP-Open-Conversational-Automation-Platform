"use client";

import React from "react";
import { KeyRound, LockOpen, Power, ShieldCheck } from "lucide-react";
import { Badge } from "@/shared/components/ui/Badge";
import { Button } from "@/shared/components/ui/Button";
import { Modal } from "@/shared/components/ui/Modal";
import { useTeamActions, type PasswordLink, type TeamMember } from "../api/useTeam";
import { PasswordLinkPanel } from "./PasswordLinkPanel";
import { ROLE_LABELS, RoleSelect } from "./RoleSelect";

function ErrorText({ error }: { error: unknown }) {
  if (!error) return null;
  return (
    <p role="alert" className="text-xs text-neutral-950">
      {error instanceof Error ? error.message : "No se pudo completar la acción."}
    </p>
  );
}

export function TeamMemberRow({ member, isSelf }: { member: TeamMember; isSelf: boolean }) {
  const { changeRole, setActive, unlock, passwordLink } = useTeamActions();
  const [link, setLink] = React.useState<PasswordLink | null>(null);
  const [confirmDeactivate, setConfirmDeactivate] = React.useState(false);
  const error = changeRole.error ?? setActive.error ?? unlock.error ?? passwordLink.error;

  return (
    <li className="flex flex-col gap-3 py-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <p className="truncate text-sm font-semibold text-neutral-950">{member.fullName || member.email}</p>
          {isSelf && <Badge tone="info">Tú</Badge>}
          {!member.isActive && <Badge tone="danger">Desactivado</Badge>}
          {member.isLocked && <Badge tone="warning">Bloqueado</Badge>}
          {member.invitationPending && <Badge tone="neutral">Invitación pendiente</Badge>}
          {member.mfaEnabled && (
            <Badge tone="neutral">
              <ShieldCheck className="h-3 w-3" aria-hidden /> MFA
            </Badge>
          )}
        </div>
        <p className="truncate text-xs text-neutral-600">{member.email}</p>
        <ErrorText error={error} />
      </div>

      <div className="flex flex-wrap items-end gap-2 sm:justify-end">
        {isSelf ? (
          <p className="text-xs text-neutral-600">{ROLE_LABELS[member.role].label}</p>
        ) : (
          <>
            <div className="w-40">
              <RoleSelect
                id={`role-${member.id}`}
                value={member.role}
                disabled={changeRole.isPending || !member.isActive}
                onChange={(role) => changeRole.mutate({ id: member.id, role })}
              />
            </div>
            {member.isLocked && (
              <Button type="button" variant="secondary" size="sm" loading={unlock.isPending} onClick={() => unlock.mutate(member.id)}>
                <LockOpen className="h-3.5 w-3.5" aria-hidden /> Desbloquear
              </Button>
            )}
            {member.isActive && (
              <Button
                type="button"
                variant="secondary"
                size="sm"
                loading={passwordLink.isPending}
                onClick={() => passwordLink.mutate(member.id, { onSuccess: setLink })}
              >
                <KeyRound className="h-3.5 w-3.5" aria-hidden />
                {member.invitationPending ? "Nuevo enlace" : "Restablecer contraseña"}
              </Button>
            )}
            <Button
              type="button"
              variant="ghost"
              size="sm"
              loading={setActive.isPending}
              onClick={() => (member.isActive ? setConfirmDeactivate(true) : setActive.mutate({ id: member.id, active: true }))}
            >
              <Power className="h-3.5 w-3.5" aria-hidden /> {member.isActive ? "Desactivar" : "Activar"}
            </Button>
          </>
        )}
      </div>

      <Modal open={link !== null} onClose={() => setLink(null)} title="Enlace para definir la contraseña">
        {link && (
          <div className="space-y-4">
            <PasswordLinkPanel link={link} recipient={member.email} />
            <div className="flex justify-end">
              <Button type="button" size="sm" onClick={() => setLink(null)}>
                Listo
              </Button>
            </div>
          </div>
        )}
      </Modal>

      <Modal open={confirmDeactivate} onClose={() => setConfirmDeactivate(false)} title={`¿Desactivar a ${member.fullName || member.email}?`}>
        <div className="space-y-4">
          <p className="text-sm text-neutral-700">
            No podrá iniciar sesión y se cerrarán sus sesiones abiertas. Sus conversaciones y su historial se conservan;
            puedes volver a activarlo cuando quieras.
          </p>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" size="sm" onClick={() => setConfirmDeactivate(false)}>
              Cancelar
            </Button>
            <Button
              type="button"
              size="sm"
              loading={setActive.isPending}
              onClick={() => setActive.mutate({ id: member.id, active: false }, { onSuccess: () => setConfirmDeactivate(false) })}
            >
              Desactivar
            </Button>
          </div>
        </div>
      </Modal>
    </li>
  );
}
