"use client";

import React from "react";
import { AlertCircle } from "lucide-react";
import { Button, Input } from "@/shared/components/ui";
import { ApiError } from "@/shared/api/types";
import type { LoginResponse } from "@/features/auth/api/authApi";
import { setupApi } from "../api/setupApi";
import {
  emptyAdminStep,
  fieldErrorsFromProblem,
  toSetupPayload,
  validateAdminStep,
  type AdminStepErrors,
  type AdminStepForm,
} from "../model/setupForm";

interface AdminStepProps {
  onCompleted: (session: LoginResponse) => Promise<void>;
}

/** Paso 1: código de configuración, organización y cuenta de administrador. */
export function AdminStep({ onCompleted }: AdminStepProps) {
  const [form, setForm] = React.useState<AdminStepForm>(emptyAdminStep);
  const [errors, setErrors] = React.useState<AdminStepErrors>({});
  const [formError, setFormError] = React.useState<string | null>(null);
  const [submitting, setSubmitting] = React.useState(false);

  const update = (field: keyof AdminStepForm) => (event: React.ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value;
    setForm((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setFormError(null);
    const found = validateAdminStep(form);
    setErrors(found);
    if (Object.keys(found).length > 0) return;

    setSubmitting(true);
    try {
      const session = await setupApi.complete(toSetupPayload(form));
      await onCompleted(session);
    } catch (error: unknown) {
      if (error instanceof ApiError && error.status === 400) {
        setErrors(fieldErrorsFromProblem(error.body));
      } else if (error instanceof ApiError && error.status === 403) {
        setErrors({ setupCode: "El código no es válido. Cópialo de nuevo desde los logs de la API." });
      } else if (error instanceof ApiError && error.status === 409) {
        setFormError("Esta instalación ya está configurada. Inicia sesión con la cuenta de administrador.");
      } else {
        setFormError("No se pudo completar la configuración. Revisa que la API esté en marcha e inténtalo de nuevo.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-5">
      {formError && (
        <div role="alert" className="flex items-start gap-2 rounded-md border-2 border-neutral-950 bg-white px-3.5 py-3 text-sm">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
          <span>{formError}</span>
        </div>
      )}

      <Input
        id="setupCode"
        label="Código de configuración"
        autoComplete="one-time-code"
        spellCheck={false}
        className="font-mono uppercase tracking-widest"
        placeholder="ABCD-EFGH-JKLM"
        value={form.setupCode}
        onChange={update("setupCode")}
        error={errors.setupCode}
        hint="Aparece en los logs de la API y al final de ./scripts/ocap-up.sh. Protege tu instalación: solo quien administra el servidor puede verlo."
      />

      <Input
        id="organizationName"
        label="Organización"
        autoComplete="organization"
        placeholder="Mi empresa"
        value={form.organizationName}
        onChange={update("organizationName")}
        error={errors.organizationName}
      />

      <div className="grid gap-5 sm:grid-cols-2">
        <Input
          id="adminName"
          label="Tu nombre"
          autoComplete="name"
          value={form.adminName}
          onChange={update("adminName")}
          error={errors.adminName}
        />
        <Input
          id="adminEmail"
          label="Correo"
          type="email"
          autoComplete="email"
          placeholder="nombre@empresa.com"
          value={form.adminEmail}
          onChange={update("adminEmail")}
          error={errors.adminEmail}
        />
      </div>

      <div className="grid gap-5 sm:grid-cols-2">
        <Input
          id="adminPassword"
          label="Contraseña"
          type="password"
          autoComplete="new-password"
          value={form.adminPassword}
          onChange={update("adminPassword")}
          error={errors.adminPassword}
          hint="Mínimo 12 caracteres, con letras y números."
        />
        <Input
          id="confirmPassword"
          label="Repite la contraseña"
          type="password"
          autoComplete="new-password"
          value={form.confirmPassword}
          onChange={update("confirmPassword")}
          error={errors.confirmPassword}
        />
      </div>

      <Button type="submit" loading={submitting} className="w-full sm:w-auto">
        {submitting ? "Creando cuenta…" : "Crear cuenta y continuar"}
      </Button>
    </form>
  );
}
