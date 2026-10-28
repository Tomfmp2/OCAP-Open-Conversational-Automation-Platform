"use client";

import React from "react";
import { Badge, Button, FormError, Modal } from "@/shared/components/ui";
import { useTeamActions, type PasswordLink, type TeamMember } from "../api/useTeam";
import { PasswordLinkPanel } from "./PasswordLinkPanel";
import { ROLE_LABELS, RoleSelect } from "./RoleSelect";

/** Estado que cambia lo que la persona puede hacer; si todo está normal, no se muestra nada. */
function MemberStatus({ member }: { member: TeamMember }) {
  if (!member.isActive) return <Badge tone="danger">Desactivado</Badge>;
  if (member.isLocked) return <Badge tone="warning">Bloqueado</Badge>;
  if (member.invitationPending) return <Badge>Invitación pendiente</Badge>;
  return null;
}

export function TeamMemberRow({ member, isSelf }: { member: TeamMember; isSelf: boolean }) {
  const [managing, setManaging] = React.useState(false);
  const name = member.fullName || member.email;

  return (
    <li className="flex items-center gap-4 px-4 py-3 sm:px-5">
      <div className="min-w-0 flex-1">
        <p className="flex flex-wrap items-center gap-2 text-sm font-medium text-neutral-950">
          <span className="truncate">{name}</span>
          {isSelf && <span className="text-xs font-normal text-neutral-600">(tú)</span>}
          <MemberStatus member={member} />
        </p>
        <p className="truncate text-sm text-neutral-600">{member.email}</p>
      </div>
      <p className="hidden text-sm text-neutral-700 sm:block">{ROLE_LABELS[member.role].label}</p>
      {!isSelf && (
        <Button variant="secondary" size="sm" onClick={() => setManaging(true)} aria-label={`Gestionar a ${name}`}>
          Gestionar
        </Button>
      )}
      <Modal open={managing} onClose={() => setManaging(false)} title={name} description={member.email}>
        {managing && <ManageMember member={member} onDone={() => setManaging(false)} />}
      </Modal>
    </li>
  );
}

function ManageMember({ member, onDone }: { member: TeamMember; onDone: () => void }) {
  const { changeRole, setActive, unlock, passwordLink } = useTeamActions();
  const [link, setLink] = React.useState<PasswordLink | null>(null);
  const [confirmDeactivate, setConfirmDeactivate] = React.useState(false);
  const error = changeRole.error ?? setActive.error ?? unlock.error ?? passwordLink.error;

  if (link) {
    return (
      <div className="space-y-5">
        <PasswordLinkPanel link={link} recipient={member.email} />
        <div className="flex justify-end">
          <Button onClick={onDone}>Listo</Button>
        </div>
      </div>
    );
  }

  if (confirmDeactivate) {
    return (
      <div className="space-y-5">
        <p className="text-sm text-neutral-700">
          No podrá iniciar sesión y se cerrarán sus sesiones abiertas. Sus conversaciones se conservan y puedes volver a
          activarlo cuando quieras.
        </p>
        <FormError error={setActive.error} />
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => setConfirmDeactivate(false)}>
            Cancelar
          </Button>
          <Button variant="danger" loading={setActive.isPending} onClick={() => setActive.mutate({ id: member.id, active: false }, { onSuccess: onDone })}>
            Desactivar
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <RoleSelect value={member.role} disabled={changeRole.isPending || !member.isActive} onChange={(role) => changeRole.mutate({ id: member.id, role })} />

      <div className="divide-y divide-neutral-200 border-y border-neutral-200">
        {member.isLocked && (
          <Action
            title="Desbloquear"
            description="Se bloqueó por demasiados intentos de contraseña."
            button={
              <Button variant="secondary" size="sm" loading={unlock.isPending} onClick={() => unlock.mutate(member.id)}>
                Desbloquear
              </Button>
            }
          />
        )}
        {member.isActive && (
          <Action
            title={member.invitationPending ? "Reenviar invitación" : "Restablecer contraseña"}
            description="Genera un enlace de un solo uso para definir la contraseña. El anterior deja de funcionar."
            button={
              <Button variant="secondary" size="sm" loading={passwordLink.isPending} onClick={() => passwordLink.mutate(member.id, { onSuccess: setLink })}>
                Generar enlace
              </Button>
            }
          />
        )}
        {member.isActive ? (
          <Action
            title="Desactivar"
            description="Quita el acceso sin borrar su historial."
            button={
              <Button variant="secondary" size="sm" onClick={() => setConfirmDeactivate(true)}>
                Desactivar
              </Button>
            }
          />
        ) : (
          <Action
            title="Activar"
            description="Vuelve a darle acceso al panel."
            button={
              <Button variant="secondary" size="sm" loading={setActive.isPending} onClick={() => setActive.mutate({ id: member.id, active: true })}>
                Activar
              </Button>
            }
          />
        )}
      </div>

      <FormError error={error} />
    </div>
  );
}

function Action({ title, description, button }: { title: string; description: string; button: React.ReactNode }) {
  return (
    <div className="flex items-center gap-4 py-3">
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-neutral-950">{title}</p>
        <p className="text-sm text-neutral-600">{description}</p>
      </div>
      {button}
    </div>
  );
}
