"use client";

import React from "react";
import { AlertCircle, Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { useSetupStatus } from "@/features/setup/api/setupApi";
import { ApiError } from "@/shared/api/apiClient";
import { Button, Input } from "@/shared/components/ui";
import Link from "next/link";
import { AuthAlert, AuthScreen } from "@/features/auth/components/AuthScreen";

export default function LoginPage() {
  const { login, completeMfa } = useAuth();
  const router = useRouter();
  const setupStatus = useSetupStatus();
  const setupRequired = setupStatus.data?.required === true;

  // Instalación sin cuentas: no hay con qué iniciar sesión, se va directo a la configuración inicial.
  React.useEffect(() => {
    if (setupRequired) router.replace("/setup");
  }, [setupRequired, router]);
  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  // Desafío del segundo factor: con él la pantalla pide el código en vez de la contraseña.
  const [mfaToken, setMfaToken] = React.useState<string | null>(null);
  const [code, setCode] = React.useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      if (mfaToken) {
        await completeMfa(mfaToken, code.trim());
      } else {
        const challenge = await login({ email: email.trim(), password });
        if (challenge) setMfaToken(challenge.mfaToken);
      }
    } catch (err: unknown) {
      if (err instanceof ApiError && err.status === 401 && mfaToken) {
        setError("Código incorrecto o vencido. Si pasaron más de 5 minutos, vuelve a ingresar tu contraseña.");
      } else if (err instanceof ApiError && err.status === 401) {
        setError(
          "Email o contraseña incorrectos."
        );
      } else if (err instanceof ApiError && err.status === 429) {
        setError("Demasiados intentos. Espera un minuto y vuelve a probar.");
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("No se pudo iniciar sesión.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthScreen
      title={mfaToken ? "Verificación en dos pasos" : "Iniciar sesión"}
      description={
        mfaToken
          ? "Tu cuenta tiene la verificación en dos pasos activada."
          : "Accede con tu cuenta de OCAP."
      }
    >
      <form onSubmit={handleSubmit} className="mt-8 space-y-4">
        {error && (
          <AuthAlert>
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
            <span>{error}</span>
          </AuthAlert>
        )}

        {mfaToken ? (
          <>
            <Input
              id="mfa-code"
              label="Código de verificación"
              inputMode="numeric"
              autoComplete="one-time-code"
              required
              autoFocus
              value={code}
              onChange={(e) => setCode(e.target.value)}
              hint="El código de 6 dígitos de tu app autenticadora, o uno de tus códigos de recuperación."
            />
            <button
              type="button"
              onClick={() => {
                setMfaToken(null);
                setCode("");
                setError(null);
              }}
              className="focus-ring text-xs text-neutral-600 underline underline-offset-2 hover:text-neutral-950"
            >
              Usar otra cuenta
            </button>
          </>
        ) : (
          <>
            <Input
              id="email"
              label="Email"
              type="email"
              required
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="admin@tudominio.com"
            />

            <Input
              id="password"
              label="Contraseña"
              type="password"
              required
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
            />
          </>
        )}

        <Button
          type="submit"
         
          loading={isSubmitting}
          className="mt-2 w-full"
        >
          {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
          <span>{isSubmitting ? "Entrando…" : mfaToken ? "Verificar" : "Entrar"}</span>
        </Button>
      </form>

      {!mfaToken && (
        <Link
          href="/forgot-password"
          className="focus-ring mt-4 self-start text-xs text-neutral-600 underline underline-offset-2 hover:text-neutral-950"
        >
          ¿Olvidaste tu contraseña?
        </Link>
      )}
    </AuthScreen>
  );
}
