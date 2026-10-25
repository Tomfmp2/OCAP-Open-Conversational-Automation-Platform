"use client";

import React from "react";
import { AlertCircle, Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { useSetupStatus } from "@/features/setup/api/setupApi";
import { ApiError } from "@/shared/api/apiClient";
import { Button, Input } from "@/shared/components/ui";

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
    <div className="relative min-h-dvh w-full bg-neutral-50 text-neutral-950">
      <div
        className="pointer-events-none absolute inset-0 opacity-[0.35]"
        style={{
          backgroundImage:
            "linear-gradient(#e5e5e5 1px, transparent 1px), linear-gradient(90deg, #e5e5e5 1px, transparent 1px)",
          backgroundSize: "48px 48px",
        }}
        aria-hidden
      />

      <div className="relative mx-auto flex min-h-dvh w-full max-w-md flex-col justify-center px-5 py-12">
        <div className="mb-10">
          <p className="text-3xl font-semibold tracking-tight">OCAP</p>
          <p className="mt-1 text-sm text-neutral-500">
            Open Conversational Automation Platform
          </p>
        </div>

        <h1 className="text-2xl font-semibold tracking-tight">Iniciar sesión</h1>
        <p className="mt-2 text-sm text-neutral-500">
          Accede con la cuenta de administrador que creaste en la configuración inicial.
        </p>

        <form onSubmit={handleSubmit} className="mt-8 space-y-4">
          {error && (
            <div
              role="alert"
              className="flex items-start gap-2 rounded-md border-2 border-neutral-950 bg-white px-3.5 py-3 text-sm text-neutral-950"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              <span>{error}</span>
            </div>
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
                className="focus-ring text-xs text-neutral-500 underline underline-offset-2 hover:text-neutral-950"
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
            size="lg"
            loading={isSubmitting}
            className="mt-2 w-full"
          >
            {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
            <span>{isSubmitting ? "Entrando…" : mfaToken ? "Verificar" : "Entrar"}</span>
          </Button>
        </form>

      </div>
    </div>
  );
}
