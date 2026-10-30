"use client";

import React from "react";
import { Button, FormError, Input, Modal } from "@/shared/components/ui";
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
    <Modal open={open} onClose={close} title={link ? "Invitación creada" : "Invitar a alguien"}>
      {link ? (
        <div className="space-y-5">
          <PasswordLinkPanel link={link} recipient={email.trim()} />
          <div className="flex justify-end">
            <Button onClick={close}>Listo</Button>
          </div>
        </div>
      ) : (
        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault();
            invite.mutate({ email: email.trim(), fullName: fullName.trim(), role }, { onSuccess: (result) => setLink(result.link) });
          }}
        >
          <Input label="Nombre" required maxLength={100} autoComplete="off" value={fullName} onChange={(e) => setFullName(e.target.value)} />
          <Input label="Correo" type="email" required autoComplete="off" value={email} onChange={(e) => setEmail(e.target.value)} />
          <RoleSelect value={role} onChange={setRole} />
          <FormError error={invite.error} />
          <div className="flex justify-end gap-2 pt-1">
            <Button variant="ghost" onClick={close}>
              Cancelar
            </Button>
            <Button type="submit" loading={invite.isPending}>
              Invitar
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}
