"use client";

import React from "react";
import { CheckCircle2, KeyRound } from "lucide-react";
import { passwordProblem } from "@/features/auth/api/passwordApi";
import { Button } from "@/shared/components/ui/Button";
import { Input } from "@/shared/components/ui/Input";
import { Surface } from "@/shared/components/ui/Surface";
import { useAccountActions } from "../api/useAccount";

export function ChangePasswordCard() {
  const { changePassword } = useAccountActions();
  const [current, setCurrent] = React.useState("");
  const [next, setNext] = React.useState("");
  const [confirm, setConfirm] = React.useState("");
  const [problem, setProblem] = React.useState<string | null>(null);

  return (
    <Surface className="space-y-4">
      <div className="flex items-center gap-2">
        <KeyRound className="h-4 w-4 text-neutral-500" aria-hidden />
        <h2 className="text-sm font-semibold text-neutral-950">Contraseña</h2>
      </div>
      <form
        className="grid gap-3 sm:grid-cols-3"
        onSubmit={(e) => {
          e.preventDefault();
          const issue = passwordProblem(next) ?? (next !== confirm ? "Las contraseñas no coinciden." : null);
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
        <Input id="current-password" label="Actual" type="password" autoComplete="current-password" required value={current} onChange={(e) => setCurrent(e.target.value)} />
        <Input id="next-password" label="Nueva" type="password" autoComplete="new-password" required value={next} onChange={(e) => setNext(e.target.value)} />
        <Input id="confirm-next-password" label="Repite la nueva" type="password" autoComplete="new-password" required value={confirm} onChange={(e) => setConfirm(e.target.value)} />
        <div className="flex flex-wrap items-center gap-3 sm:col-span-3">
          <Button type="submit" size="sm" loading={changePassword.isPending}>
            Cambiar contraseña
          </Button>
          <p className="text-xs text-neutral-500">Al menos 12 caracteres, con letras y números.</p>
        </div>
        {(problem || changePassword.isError) && (
          <p role="alert" className="text-xs text-neutral-950 sm:col-span-3">
            {problem ?? (changePassword.error instanceof Error ? changePassword.error.message : "No se pudo cambiar.")}
          </p>
        )}
        {changePassword.isSuccess && !problem && (
          <p role="status" className="flex items-center gap-1.5 text-xs text-neutral-700 sm:col-span-3">
            <CheckCircle2 className="h-3.5 w-3.5" aria-hidden /> Contraseña actualizada.
          </p>
        )}
      </form>
    </Surface>
  );
}
