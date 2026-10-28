"use client";

import React from "react";
import { CheckCircle2 } from "lucide-react";
import { passwordProblem } from "@/features/auth/api/passwordApi";
import { Button, FormError, Input, Section } from "@/shared/components/ui";
import { useAccountActions } from "../api/useAccount";

export function ChangePasswordCard() {
  const { changePassword } = useAccountActions();
  const [current, setCurrent] = React.useState("");
  const [next, setNext] = React.useState("");
  const [confirm, setConfirm] = React.useState("");
  const [problem, setProblem] = React.useState<string | null>(null);

  return (
    <Section title="Contraseña" description="Al menos 12 caracteres, con letras y números.">
      <form
        className="max-w-sm space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          const issue = passwordProblem(next) ?? (next !== confirm ? "Las contraseñas nuevas no coinciden." : null);
          setProblem(issue);
          if (issue) return;
          changePassword.mutate(
            { currentPassword: current, newPassword: next },
            {
              onSuccess: () => {
                setCurrent("");
                setNext("");
                setConfirm("");
              },
            }
          );
        }}
      >
        <Input label="Contraseña actual" type="password" autoComplete="current-password" required value={current} onChange={(e) => setCurrent(e.target.value)} />
        <Input label="Nueva contraseña" type="password" autoComplete="new-password" required value={next} onChange={(e) => setNext(e.target.value)} />
        <Input label="Repite la nueva contraseña" type="password" autoComplete="new-password" required value={confirm} onChange={(e) => setConfirm(e.target.value)} />
        <FormError error={problem ? new Error(problem) : changePassword.error} />
        {changePassword.isSuccess && !problem && (
          <p role="status" className="flex items-center gap-2 text-sm text-success">
            <CheckCircle2 className="h-4 w-4" aria-hidden /> Contraseña cambiada.
          </p>
        )}
        <Button type="submit" loading={changePassword.isPending}>
          Cambiar contraseña
        </Button>
      </form>
    </Section>
  );
}
