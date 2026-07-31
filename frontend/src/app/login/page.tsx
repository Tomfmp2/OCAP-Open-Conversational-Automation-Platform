"use client";

import React from "react";
import Link from "next/link";
import { AlertCircle, Loader2, Sparkles } from "lucide-react";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { ApiError } from "@/shared/api/apiClient";
import { Button, Input } from "@/shared/components/ui";

export default function LoginPage() {
  const { login } = useAuth();
  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await login({ email: email.trim(), password });
    } catch (err: unknown) {
      if (err instanceof ApiError && err.status === 401) {
        setError(
          "Email o contraseña incorrectos. Usa la cuenta del instalador o vuelve a /installer y pulsa Aplicar para actualizar el admin."
        );
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("No se pudo iniciar sesión. Verifica tus credenciales.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="relative min-h-dvh w-full overflow-x-hidden bg-zinc-950 text-zinc-50">
      <div
        className="pointer-events-none absolute inset-0 opacity-80"
        style={{
          backgroundImage:
            "radial-gradient(ellipse 80% 60% at 10% 20%, rgba(255,255,255,0.08), transparent 50%), radial-gradient(ellipse 60% 50% at 90% 80%, rgba(255,255,255,0.05), transparent 45%)",
        }}
        aria-hidden
      />
      <div
        className="pointer-events-none absolute inset-0 opacity-[0.12]"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml,%3Csvg width='72' height='72' viewBox='0 0 72 72' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='%23ffffff' fill-opacity='0.45'%3E%3Cpath d='M36 4l4 8h-8l4-8zm0 56l4 8h-8l4-8zM4 36l8-4v8l-8-4zm56 0l8-4v8l-8-4z'/%3E%3C/g%3E%3C/svg%3E\")",
        }}
        aria-hidden
      />

      <div className="relative mx-auto grid min-h-dvh w-full max-w-6xl lg:grid-cols-[minmax(0,1fr)_minmax(0,1.05fr)]">
        <section className="flex flex-col justify-center px-5 py-10 sm:px-8 lg:px-12 lg:py-16">
          <div className="mb-10 flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-zinc-50 text-zinc-950">
              <Sparkles className="h-5 w-5" />
            </div>
            <div>
              <p className="text-lg font-semibold tracking-tight">OCAP</p>
              <p className="text-[11px] uppercase tracking-[0.18em] text-zinc-500">
                Control Plane
              </p>
            </div>
          </div>

          <h1 className="max-w-md text-3xl font-semibold tracking-tight sm:text-4xl">
            Iniciar sesión
          </h1>
          <p className="mt-3 max-w-md text-sm leading-relaxed text-zinc-400">
            Accede al panel de agentes, canales y automatización. Si acabas de
            instalar, usa el email y contraseña que definiste en el instalador.
          </p>

          <form onSubmit={handleSubmit} className="mt-8 w-full max-w-md space-y-4">
            {error && (
              <div
                role="alert"
                className="flex items-start gap-2 rounded-2xl border border-red-500/30 bg-red-500/10 px-3.5 py-3 text-sm text-red-200"
              >
                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
                <span>{error}</span>
              </div>
            )}

            <Input
              id="email"
              label="Email"
              type="email"
              required
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="admin@tudominio.com"
              className="rounded-2xl border-zinc-700 bg-zinc-900 text-zinc-50 placeholder:text-zinc-500"
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
              className="rounded-2xl border-zinc-700 bg-zinc-900 text-zinc-50 placeholder:text-zinc-500"
            />

            <Button
              type="submit"
              variant="mono"
              size="lg"
              loading={isSubmitting}
              className="mt-2 w-full rounded-2xl bg-zinc-50 text-zinc-950 hover:bg-white"
            >
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
              <span>{isSubmitting ? "Entrando…" : "Entrar"}</span>
            </Button>
          </form>

          <p className="mt-8 max-w-md text-sm text-zinc-500">
            ¿Primera vez o no te deja entrar?{" "}
            <Link
              href="/installer"
              className="font-medium text-zinc-200 underline decoration-zinc-600 underline-offset-4 hover:text-white"
            >
              Abrir instalador
            </Link>{" "}
            y vuelve a aplicar el admin.
          </p>
        </section>

        <aside className="relative hidden min-h-dvh border-l border-zinc-800/80 lg:block">
          <div className="absolute inset-0 bg-gradient-to-b from-zinc-900/40 via-zinc-950 to-zinc-950" />
          <div className="relative flex h-full flex-col justify-between p-10 xl:p-14">
            <div>
              <p className="text-xs uppercase tracking-[0.2em] text-zinc-500">
                Open Conversational Automation
              </p>
              <p className="mt-4 max-w-sm text-2xl font-semibold tracking-tight text-zinc-100">
                Un solo panel para orquestar agentes, canales y workflows.
              </p>
            </div>
            <ul className="space-y-3 text-sm text-zinc-400">
              <li>Agentes y herramientas Google Workspace</li>
              <li>Canales Telegram / WhatsApp</li>
              <li>Knowledge, monitoreo y seguridad</li>
            </ul>
          </div>
        </aside>
      </div>
    </div>
  );
}
