"use client";

import React from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { AlertCircle, CheckCircle2 } from "lucide-react";
import { AuthAlert, AuthScreen } from "@/features/auth/components/AuthScreen";
import { passwordApi, passwordProblem } from "@/features/auth/api/passwordApi";
import { Button, Input } from "@/shared/components/ui";

/** Definir la contraseña con el enlace de una invitación al equipo o de una recuperación. */
export default function AcceptPage() {
  return (
    <React.Suspense fallback={null}>
      <AcceptForm />
    </React.Suspense>
  );
}

function AcceptForm() {
  const token = useSearchParams().get("token") ?? "";
  const [password, setPassword] = React.useState("");
  const [confirm, setConfirm] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [done, setDone] = React.useState(false);
  const [submitting, setSubmitting] = React.useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const problem = passwordProblem(password) ?? (password !== confirm ? "Las contraseñas no coinciden." : null);
    if (problem) {
      setError(problem);
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      await passwordApi.setup(token, password);
      setDone(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo guardar la contraseña.");
    } finally {
      setSubmitting(false);
    }
  };

  if (!token) {
    return (
      <AuthScreen title="Enlace incompleto" description="Abre el enlace completo que recibiste, o pide uno nuevo a un administrador.">
        <Link href="/login" className="focus-ring mt-6 text-sm underline underline-offset-2">
          Ir a iniciar sesión
        </Link>
      </AuthScreen>
    );
  }

  if (done) {
    return (
      <AuthScreen title="Contraseña guardada" description="Ya puedes entrar al panel con tu correo y tu nueva contraseña.">
        <p className="mt-6 flex items-center gap-2 text-sm text-neutral-700">
          <CheckCircle2 className="h-4 w-4" aria-hidden /> Por seguridad cerramos cualquier otra sesión abierta.
        </p>
        <Link
          href="/login"
          className="focus-ring mt-6 inline-flex h-10 items-center justify-center rounded-md bg-neutral-950 px-4 text-sm font-medium text-white hover:bg-neutral-800"
        >
          Iniciar sesión
        </Link>
      </AuthScreen>
    );
  }

  return (
    <AuthScreen title="Define tu contraseña" description="Al menos 12 caracteres, combinando letras y números.">
      <form onSubmit={submit} className="mt-8 space-y-4">
        {error && (
          <AuthAlert>
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
            <span>{error}</span>
          </AuthAlert>
        )}
        <Input
          id="new-password"
          label="Nueva contraseña"
          type="password"
          autoComplete="new-password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <Input
          id="confirm-password"
          label="Repite la contraseña"
          type="password"
          autoComplete="new-password"
          required
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
        />
        <Button type="submit" size="lg" loading={submitting} className="mt-2 w-full">
          Guardar contraseña
        </Button>
      </form>
    </AuthScreen>
  );
}
