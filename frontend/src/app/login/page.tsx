"use client";

import React from "react";
import Link from "next/link";
import { AlertCircle, Loader2 } from "lucide-react";
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
          "Email o contraseña incorrectos. Usa la cuenta del instalador o vuelve a /installer y aplica de nuevo."
        );
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
          Accede al panel con el email y contraseña definidos en el instalador.
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

          <Button
            type="submit"
            size="lg"
            loading={isSubmitting}
            className="mt-2 w-full"
          >
            {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
            <span>{isSubmitting ? "Entrando…" : "Entrar"}</span>
          </Button>
        </form>

        <p className="mt-8 text-sm text-neutral-500">
          ¿Primera instalación?{" "}
          <Link
            href="/installer"
            className="font-medium text-neutral-950 underline underline-offset-4"
          >
            Abrir instalador
          </Link>
        </p>
      </div>
    </div>
  );
}
