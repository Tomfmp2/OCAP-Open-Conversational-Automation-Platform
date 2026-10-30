"use client";

import React from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { CheckCircle2, Loader2 } from "lucide-react";
import { Button, ErrorState } from "@/shared/components/ui";
import { useAuth } from "@/features/auth/context/AuthProvider";
import type { LoginResponse } from "@/features/auth/api/authApi";
import { useSetupStatus } from "../api/setupApi";
import { AdminStep } from "./AdminStep";
import { AiStep } from "./AiStep";

const STEPS = [
  { id: "admin", title: "Cuenta" },
  { id: "ai", title: "IA" },
  { id: "done", title: "Listo" },
] as const;
type StepId = (typeof STEPS)[number]["id"];

const STEP_COPY: Record<StepId, { title: string; description: string }> = {
  admin: {
    title: "Crea tu cuenta de administrador",
    description: "Es la primera cuenta de esta instalación y tendrá todos los permisos de administración.",
  },
  ai: {
    title: "Conecta un proveedor de IA",
    description: "Opcional. El asistente lo usa para responder; puedes cambiarlo cuando quieras en «IA y modelos».",
  },
  done: {
    title: "OCAP está listo",
    description: "Tu cuenta quedó creada y la sesión está iniciada.",
  },
};

function StepIndicator({ current }: { current: StepId }) {
  const currentIndex = STEPS.findIndex((s) => s.id === current);
  return (
    <ol className="flex items-center gap-3 text-xs" aria-label="Pasos de la configuración">
      {STEPS.map((step, index) => {
        const state = index < currentIndex ? "done" : index === currentIndex ? "current" : "next";
        return (
          <li key={step.id} className="flex items-center gap-2" aria-current={state === "current" ? "step" : undefined}>
            <span
              className={
                state === "next"
                  ? "flex h-6 w-6 items-center justify-center rounded-full border border-neutral-300 text-neutral-600"
                  : "flex h-6 w-6 items-center justify-center rounded-full bg-neutral-950 text-neutral-50"
              }
            >
              {state === "done" ? <CheckCircle2 className="h-3.5 w-3.5" aria-hidden /> : index + 1}
            </span>
            <span className={state === "next" ? "text-neutral-600" : "font-medium text-neutral-950"}>
              {step.title}
            </span>
            {index < STEPS.length - 1 && <span className="h-px w-6 bg-neutral-300" aria-hidden />}
          </li>
        );
      })}
    </ol>
  );
}

/**
 * Configuración inicial en tres pasos. Solo el primero es obligatorio; la infraestructura (puertos, base de datos,
 * secretos) ya la resolvió scripts/ocap-setup.sh, así que aquí no se pregunta nada técnico.
 */
export function SetupWizard() {
  const router = useRouter();
  const { adoptSession } = useAuth();
  const status = useSetupStatus();
  const [step, setStep] = React.useState<StepId>("admin");

  // Instalación ya configurada: la configuración inicial no aplica y se va al login.
  const alreadyConfigured = step === "admin" && status.data?.required === false;
  React.useEffect(() => {
    if (alreadyConfigured) router.replace("/login");
  }, [alreadyConfigured, router]);

  const handleAdminCreated = async (session: LoginResponse) => {
    await adoptSession(session);
    setStep("ai");
  };

  if (status.isLoading || alreadyConfigured) {
    return (
      <div className="flex items-center gap-2 text-sm text-neutral-600" role="status">
        <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> Comprobando la instalación…
      </div>
    );
  }

  if (status.isError) {
    return (
      <ErrorState
        title="No se pudo contactar la API"
        message="Comprueba que OCAP esté en marcha (./scripts/ocap-up.sh) y recarga la página."
        onRetry={() => void status.refetch()}
      />
    );
  }

  const copy = STEP_COPY[step];

  return (
    <div className="space-y-8">
      <StepIndicator current={step} />

      <div className="space-y-2">
        <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">{copy.title}</h1>
        <p className="max-w-xl text-sm leading-relaxed text-neutral-600">{copy.description}</p>
      </div>

      {step === "admin" && <AdminStep onCompleted={handleAdminCreated} />}
      {step === "ai" && <AiStep onNext={() => setStep("done")} />}
      {step === "done" && (
        <div className="space-y-6">
          <ul className="space-y-2 text-sm text-neutral-700">
            <li>· Conecta WhatsApp, Telegram o el WebChat desde <strong>Canales</strong>.</li>
            <li>· Sube documentos a <strong>Conocimiento</strong> para que el asistente responda con tu información.</li>
            <li>· Cambia el proveedor o el modelo en <strong>IA y modelos</strong>.</li>
          </ul>
          <div className="flex flex-col gap-3 sm:flex-row">
            <Button onClick={() => router.replace("/")}>Ir al asistente</Button>
            <Link
              href="/channels"
              className="inline-flex items-center justify-center rounded-md border border-neutral-300 bg-white px-5 py-2.5 text-sm font-medium text-neutral-900 hover:bg-neutral-100 focus-ring"
            >
              Conectar un canal
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
