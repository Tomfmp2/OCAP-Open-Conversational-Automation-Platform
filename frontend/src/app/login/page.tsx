"use client";

import React from "react";
import { AlertCircle, Loader2, Sparkles } from "lucide-react";
import { useAuth } from "@/features/auth/context/AuthProvider";
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
      const message =
        err instanceof Error
          ? err.message
          : "No se pudo iniciar sesión. Verifique sus credenciales.";
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-zinc-200 p-4">
      <div
        className="absolute inset-0 bg-[url('data:image/svg+xml,%3Csvg width=\\'60\\' height=\\'60\\' viewBox=\\'0 0 60 60\\' xmlns=\\'http://www.w3.org/2000/svg\\'%3E%3Cg fill=\\'none\\' fill-rule=\\'evenodd\\'%3E%3Cg fill=\\'%239C92AC\\' fill-opacity=\\'0.08\\'%3E%3Cpath d=\\'M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z\\'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E')] opacity-60"
        aria-hidden
      />
      <div className="absolute inset-0 bg-gradient-to-br from-zinc-100 via-zinc-200 to-zinc-300" />

      <div className="relative grid w-full max-w-5xl overflow-hidden rounded-[28px] border border-white/70 bg-white shadow-2xl md:grid-cols-2">
        <div className="flex flex-col justify-center p-8 sm:p-10">
          <div className="mb-8 flex items-center gap-2">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-zinc-950 text-white">
              <Sparkles className="h-4 w-4" />
            </div>
            <span className="text-sm font-bold tracking-tight text-zinc-950">OCAP</span>
          </div>

          <h1 className="text-3xl font-bold tracking-tight text-zinc-950">Sign In</h1>
          <p className="mt-2 font-mono text-xs text-zinc-500">
            Continue to access your dashboard
          </p>

          <form onSubmit={handleSubmit} className="mt-8 space-y-4">
            {error && (
              <div
                role="alert"
                className="flex items-center gap-2 rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-600"
              >
                <AlertCircle className="h-4 w-4 shrink-0" />
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
              placeholder="Enter your email"
              className="rounded-2xl border-zinc-300 font-mono"
            />

            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <label
                  htmlFor="password"
                  className="block text-xs font-semibold tracking-wide text-zinc-700"
                >
                  Password
                </label>
                <span className="font-mono text-[11px] text-zinc-400 underline decoration-zinc-300">
                  Managed by admin
                </span>
              </div>
              <input
                id="password"
                type="password"
                required
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full rounded-2xl border border-zinc-300 bg-white px-3.5 py-2.5 font-mono text-sm text-zinc-900 placeholder:text-zinc-400 focus-ring"
              />
            </div>

            <Button
              type="submit"
              variant="mono"
              size="lg"
              loading={isSubmitting}
              className="mt-2 w-full font-mono"
            >
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
              <span>{isSubmitting ? "Signing in..." : "Sign In"}</span>
            </Button>
          </form>

          <p className="mt-8 text-center font-mono text-xs text-zinc-500">
            Plataforma de agentes, canales y automatización OCAP
          </p>
        </div>

        <div className="relative hidden overflow-hidden border-l border-zinc-100 bg-white md:block">
          <div
            className="absolute inset-0 opacity-90"
            style={{
              backgroundImage:
                "radial-gradient(circle at 20% 20%, rgba(37,99,235,0.18), transparent 35%), radial-gradient(circle at 80% 70%, rgba(24,24,27,0.08), transparent 40%)",
            }}
          />
          <svg
            className="absolute inset-0 h-full w-full"
            viewBox="0 0 400 560"
            xmlns="http://www.w3.org/2000/svg"
            aria-hidden
          >
            {Array.from({ length: 28 }).map((_, row) =>
              Array.from({ length: 20 }).map((__, col) => {
                const density = Math.abs(Math.sin(row * 0.45) * Math.cos(col * 0.35));
                if (density < 0.25) return null;
                const size = 3 + density * 7;
                return (
                  <rect
                    key={`${row}-${col}`}
                    x={20 + col * 18}
                    y={30 + row * 18}
                    width={size}
                    height={size}
                    rx="1"
                    fill="#09090b"
                    opacity={0.15 + density * 0.75}
                    transform={`rotate(45 ${20 + col * 18} ${30 + row * 18})`}
                  />
                );
              })
            )}
          </svg>
          <div className="absolute right-8 bottom-8 left-8 rounded-2xl border border-zinc-200 bg-white/80 p-4 backdrop-blur">
            <p className="text-xs font-semibold text-zinc-900">OCAP Control Plane</p>
            <p className="mt-1 font-mono text-[11px] text-zinc-500">
              Agents · Channels · Workflows · Knowledge · Security
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
