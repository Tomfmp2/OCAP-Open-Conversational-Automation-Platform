"use client";

import React from "react";
import { AlertCircle } from "lucide-react";
import { Modal } from "@/shared/components/ui/Modal";
import { Input } from "@/shared/components/ui/Input";
import { Button } from "@/shared/components/ui/Button";
import { useTeamActions, type PasswordLink, type TeamRole } from "../api/useTeam";
import { PasswordLinkPanel } from "./PasswordLinkPanel";
import { RoleSelect } from "./RoleSelect";

export function InviteMemberModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { invite } = useTeamActions();
  const [fullName, setFullName] = React.useState("");
  const [email, setEmail] = React.useState("");
  const [role, setRole] = React.useState<TeamRole>("Operator");
  const [link, setLink] = React.useState<PasswordLink | null>(null);

  const close = () => {
    setFullName("");
    setEmail("");
    setRole("Operator");
    setLink(null);
    invite.reset();
    onClose();
  };

  return (
    <Modal open={open} onClose={close} title={link ? "Invitación lista" : "Invitar al equipo"}>
      {link ? (
        <div className="space-y-4">
          <PasswordLinkPanel link={link} recipient={email.trim()} />
          <div className="flex justify-end">
            <Button type="button" size="sm" onClick={close}>
              Listo
            </Button>
          </div>
        </div>
      ) : (
        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault();
            invite.mutate(
              { email: email.trim(), fullName: fullName.trim(), role },
              { onSuccess: (result) => setLink(result.link) }
            );
          }}
        >
          <Input id="invite-name" label="Nombre" required maxLength={100} value={fullName} onChange={(e) => setFullName(e.target.value)} />
          <Input id="invite-email" label="Correo" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          <RoleSelect id="invite-role" value={role} onChange={setRole} />
          {invite.isError && (
            <p role="alert" className="flex items-center gap-2 text-xs text-neutral-950">
              <AlertCircle className="h-3.5 w-3.5 shrink-0" aria-hidden />
              {invite.error instanceof Error ? invite.error.message : "No se pudo invitar."}
            </p>
          )}
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" size="sm" onClick={close}>
              Cancelar
            </Button>
            <Button type="submit" size="sm" loading={invite.isPending}>
              Invitar
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}
