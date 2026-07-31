"use client";

import React from "react";
import { ArrowLeft, ArrowRight, CheckCircle2, Loader2 } from "lucide-react";
import { Badge, Button, Input, Surface } from "@/shared/components/ui";
import { InstallerWizardSteps } from "./InstallerWizardSteps";
import {
  useInstallerDiagnostic,
  useInstallerSetupMutation,
  useInstallerStatus,
} from "../api/useInstallerSetup";
import {
  INSTALLER_STEPS,
  defaultInstallerForm,
  validateInstallerStep,
  type InstallerFormState,
  type InstallerStepId,
} from "../model/installerForm";

function StepRail({
  current,
  completedThrough,
}: {
  current: InstallerStepId;
  completedThrough: number;
}) {
  const currentIndex = INSTALLER_STEPS.findIndex((s) => s.id === current);
  return (
    <ol className="flex flex-wrap gap-2">
      {INSTALLER_STEPS.map((step, index) => {
        const done = index < completedThrough || index < currentIndex;
        const active = step.id === current;
        return (
          <li key={step.id}>
            <Badge tone={active ? "info" : done ? "success" : "neutral"}>
              {index + 1}. {step.title}
            </Badge>
          </li>
        );
      })}
    </ol>
  );
}

export function InstallerWizard() {
  const { data: status, isLoading: statusLoading } = useInstallerStatus();
  const [step, setStep] = React.useState<InstallerStepId>("mode");
  const [form, setForm] = React.useState<InstallerFormState>(defaultInstallerForm);
  const [error, setError] = React.useState<string | null>(null);
  const [setupResult, setSetupResult] = React.useState<string | null>(null);
  const [forceWizard, setForceWizard] = React.useState(false);
  const setupMutation = useInstallerSetupMutation();

  const showDiagnosticOnly = Boolean(status?.completed) && !forceWizard;
  const diagnosticQuery = useInstallerDiagnostic(showDiagnosticOnly || step === "diagnostic");

  React.useEffect(() => {
    if (status?.completed && !forceWizard) {
      setStep("diagnostic");
    }
  }, [status?.completed, forceWizard]);

  const patch = <K extends keyof InstallerFormState>(key: K, value: InstallerFormState[K]) => {
    setForm((prev) => ({ ...prev, [key]: value }));
    setError(null);
  };

  const currentIndex = INSTALLER_STEPS.findIndex((s) => s.id === step);

  const goNext = () => {
    const validationError = validateInstallerStep(step, form);
    if (validationError) {
      setError(validationError);
      return;
    }
    const next = INSTALLER_STEPS[currentIndex + 1];
    if (next) setStep(next.id);
  };

  const goBack = () => {
    const prev = INSTALLER_STEPS[currentIndex - 1];
    if (prev) setStep(prev.id);
  };

  const submit = async () => {
    const validationError = validateInstallerStep("channels", form) ||
      validateInstallerStep("admin", form) ||
      validateInstallerStep("database", form) ||
      validateInstallerStep("network", form) ||
      validateInstallerStep("google", form) ||
      validateInstallerStep("ai", form);
    if (validationError) {
      setError(validationError);
      return;
    }

    try {
      const result = await setupMutation.mutateAsync(form);
      setSetupResult(
        `${result.message}${result.requiresRestart ? ` ${result.restartHint}` : ""}`
      );
      setStep("diagnostic");
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo completar la instalación.");
    }
  };

  if (statusLoading) {
    return (
      <Surface className="flex items-center gap-2 p-6 text-sm text-zinc-400">
        <Loader2 className="h-4 w-4 animate-spin" />
        Cargando estado del instalador…
      </Surface>
    );
  }

  if (showDiagnosticOnly) {
    return (
      <div className="space-y-4">
        <Surface className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-2 text-sm text-emerald-400">
            <CheckCircle2 className="h-4 w-4" />
            Instalación marcada como completa.
          </div>
          <Button variant="secondary" size="sm" onClick={() => setForceWizard(true)}>
            Reconfigurar
          </Button>
        </Surface>
        {diagnosticQuery.data ? (
          <InstallerWizardSteps
            steps={diagnosticQuery.data.steps}
            isSystemReady={diagnosticQuery.data.isSystemReady}
            isValidating={diagnosticQuery.isFetching}
            lastCheckedAt={diagnosticQuery.data.timestamp}
            onValidate={() => void diagnosticQuery.refetch()}
          />
        ) : null}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <StepRail current={step} completedThrough={currentIndex} />

      <Surface variant="glass" className="space-y-5 p-5">
        {step === "mode" && (
          <div className="space-y-4">
            <h2 className="text-sm font-semibold text-zinc-100">¿Dónde vas a desplegar OCAP?</h2>
            <div className="grid gap-3 sm:grid-cols-2">
              {(["Local", "Web"] as const).map((target) => (
                <button
                  key={target}
                  type="button"
                  onClick={() => patch("target", target)}
                  className={`rounded-xl border p-4 text-left text-sm transition ${
                    form.target === target
                      ? "border-zinc-100 bg-zinc-100 text-zinc-900"
                      : "border-zinc-800 bg-zinc-950 text-zinc-300 hover:border-zinc-600"
                  }`}
                >
                  <p className="font-semibold">{target === "Local" ? "Local" : "Web / servidor"}</p>
                  <p className="mt-1 text-xs opacity-70">
                    {target === "Local"
                      ? "Pide puerto del panel admin (frontend) y de la API."
                      : "Pide URL pública de la API y del panel para CORS/OAuth."}
                  </p>
                </button>
              ))}
            </div>
          </div>
        )}

        {step === "network" && form.target === "Local" && (
          <div className="grid gap-4 sm:grid-cols-2">
            <Input
              label="Puerto del frontend (panel admin)"
              type="number"
              value={form.frontendHostPort}
              onChange={(e) => patch("frontendHostPort", Number(e.target.value))}
            />
            <Input
              label="Puerto de la API"
              type="number"
              value={form.apiHostPort}
              onChange={(e) => patch("apiHostPort", Number(e.target.value))}
            />
          </div>
        )}

        {step === "network" && form.target === "Web" && (
          <div className="grid gap-4">
            <Input
              label="URL pública de la API"
              value={form.publicApiUrl}
              onChange={(e) => patch("publicApiUrl", e.target.value)}
              placeholder="https://api.tudominio.com"
            />
            <Input
              label="URL pública del panel admin"
              value={form.publicPanelUrl}
              onChange={(e) => patch("publicPanelUrl", e.target.value)}
              placeholder="https://app.tudominio.com"
              hint="Mismo frontend del proyecto; se usa para CORS y redirects OAuth."
            />
          </div>
        )}

        {step === "database" && (
          <div className="grid gap-4 sm:grid-cols-2">
            <Input label="Host" value={form.postgresHost} onChange={(e) => patch("postgresHost", e.target.value)} />
            <Input
              label="Puerto"
              type="number"
              value={form.postgresPort}
              onChange={(e) => patch("postgresPort", Number(e.target.value))}
            />
            <Input label="Base de datos" value={form.postgresDbName} onChange={(e) => patch("postgresDbName", e.target.value)} />
            <Input label="Usuario" value={form.postgresUsername} onChange={(e) => patch("postgresUsername", e.target.value)} />
            <Input
              className="sm:col-span-2"
              label="Contraseña"
              type="password"
              value={form.postgresPassword}
              onChange={(e) => patch("postgresPassword", e.target.value)}
            />
          </div>
        )}

        {step === "admin" && (
          <div className="grid gap-4 sm:grid-cols-2">
            <Input label="Organización" value={form.tenantName} onChange={(e) => patch("tenantName", e.target.value)} />
            <Input label="Slug" value={form.tenantSlug} onChange={(e) => patch("tenantSlug", e.target.value)} />
            <Input
              className="sm:col-span-2"
              label="Email admin"
              type="email"
              value={form.adminEmail}
              onChange={(e) => patch("adminEmail", e.target.value)}
            />
            <Input
              label="Contraseña admin"
              type="password"
              value={form.adminPassword}
              onChange={(e) => patch("adminPassword", e.target.value)}
            />
            <Input
              label="Confirmar contraseña"
              type="password"
              value={form.adminPasswordConfirm}
              onChange={(e) => patch("adminPasswordConfirm", e.target.value)}
            />
          </div>
        )}

        {step === "google" && (
          <div className="space-y-4">
            <label className="flex items-center gap-2 text-sm text-zinc-300">
              <input
                type="checkbox"
                checked={form.enableGoogleWorkspace}
                onChange={(e) => patch("enableGoogleWorkspace", e.target.checked)}
              />
              Activar Google Workspace (Sheets / Calendar / Gmail)
            </label>
            {form.enableGoogleWorkspace && (
              <div className="grid gap-4">
                <Input
                  label="Client ID"
                  value={form.googleClientId}
                  onChange={(e) => patch("googleClientId", e.target.value)}
                />
                <Input
                  label="Client Secret"
                  type="password"
                  value={form.googleClientSecret}
                  onChange={(e) => patch("googleClientSecret", e.target.value)}
                />
                <Input
                  label="Redirect URI (opcional)"
                  value={form.googleRedirectUri}
                  onChange={(e) => patch("googleRedirectUri", e.target.value)}
                  hint="Si se deja vacío se deriva de la URL de la API."
                />
              </div>
            )}
          </div>
        )}

        {step === "ai" && (
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5 sm:col-span-2">
              <p className="text-xs font-semibold text-zinc-300">Proveedor</p>
              <div className="flex flex-wrap gap-2">
                {(["OpenAI", "Gemini", "Claude", "Ollama"] as const).map((p) => (
                  <Button
                    key={p}
                    type="button"
                    size="sm"
                    variant={form.aiProvider === p ? "primary" : "secondary"}
                    onClick={() => {
                      patch("aiProvider", p);
                      if (p === "Gemini") patch("aiModelName", "gemini-1.5-flash");
                      if (p === "OpenAI") patch("aiModelName", "gpt-4o");
                      if (p === "Claude") patch("aiModelName", "claude-3-5-sonnet-latest");
                      if (p === "Ollama") patch("aiModelName", "llama3");
                    }}
                  >
                    {p}
                  </Button>
                ))}
              </div>
            </div>
            <Input
              label="Modelo"
              value={form.aiModelName}
              onChange={(e) => patch("aiModelName", e.target.value)}
            />
            <Input
              label="API key"
              type="password"
              value={form.aiApiKey}
              onChange={(e) => patch("aiApiKey", e.target.value)}
              hint={form.aiProvider === "Ollama" ? "Opcional para Ollama." : undefined}
            />
            <Input
              className="sm:col-span-2"
              label="Base URL"
              value={form.aiBaseUrl}
              onChange={(e) => patch("aiBaseUrl", e.target.value)}
              hint="Obligatoria para Ollama (ej. http://localhost:11434)."
            />
          </div>
        )}

        {step === "channels" && (
          <div className="space-y-5">
            <div className="space-y-3">
              <label className="flex items-center gap-2 text-sm text-zinc-300">
                <input
                  type="checkbox"
                  checked={form.enableWhatsApp}
                  onChange={(e) => patch("enableWhatsApp", e.target.checked)}
                />
                WhatsApp (Evolution API)
              </label>
              {form.enableWhatsApp && (
                <div className="grid gap-3 sm:grid-cols-2">
                  <Input
                    label="URL Evolution"
                    value={form.evolutionApiUrl}
                    onChange={(e) => patch("evolutionApiUrl", e.target.value)}
                  />
                  <Input
                    label="API key"
                    type="password"
                    value={form.evolutionApiKey}
                    onChange={(e) => patch("evolutionApiKey", e.target.value)}
                  />
                </div>
              )}
            </div>
            <div className="space-y-3">
              <label className="flex items-center gap-2 text-sm text-zinc-300">
                <input
                  type="checkbox"
                  checked={form.enableTelegram}
                  onChange={(e) => patch("enableTelegram", e.target.checked)}
                />
                Telegram
              </label>
              {form.enableTelegram && (
                <Input
                  label="Bot token"
                  type="password"
                  value={form.telegramBotToken}
                  onChange={(e) => patch("telegramBotToken", e.target.value)}
                />
              )}
            </div>
          </div>
        )}

        {step === "review" && (
          <div className="space-y-3 text-sm text-zinc-300">
            <p>
              <span className="text-zinc-500">Modo:</span> {form.target}
            </p>
            <p>
              <span className="text-zinc-500">Red:</span>{" "}
              {form.target === "Local"
                ? `frontend :${form.frontendHostPort}, API :${form.apiHostPort}`
                : `${form.publicPanelUrl} → ${form.publicApiUrl}`}
            </p>
            <p>
              <span className="text-zinc-500">Postgres:</span> {form.postgresUsername}@{form.postgresHost}:{form.postgresPort}/{form.postgresDbName}
            </p>
            <p>
              <span className="text-zinc-500">Admin:</span> {form.adminEmail} ({form.tenantSlug})
            </p>
            <p>
              <span className="text-zinc-500">Google:</span>{" "}
              {form.enableGoogleWorkspace ? "activado" : "omitido"}
            </p>
            <p>
              <span className="text-zinc-500">IA:</span> {form.aiProvider} / {form.aiModelName}
            </p>
            {setupResult && (
              <p className="rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-3 text-xs text-emerald-300">
                {setupResult}
              </p>
            )}
          </div>
        )}

        {step === "diagnostic" && (
          <div className="space-y-4">
            {setupResult && (
              <p className="rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-3 text-xs text-emerald-300">
                {setupResult}
              </p>
            )}
            {diagnosticQuery.data ? (
              <InstallerWizardSteps
                steps={diagnosticQuery.data.steps}
                isSystemReady={diagnosticQuery.data.isSystemReady}
                isValidating={diagnosticQuery.isFetching}
                lastCheckedAt={diagnosticQuery.data.timestamp}
                onValidate={() => void diagnosticQuery.refetch()}
              />
            ) : (
              <p className="text-sm text-zinc-400">Cargando diagnóstico…</p>
            )}
          </div>
        )}

        {error && (
          <p className="rounded-lg border border-red-500/30 bg-red-500/10 px-3 py-2 text-xs text-red-300">
            {error}
          </p>
        )}

        {step !== "diagnostic" && (
          <div className="flex items-center justify-between gap-3 border-t border-zinc-800 pt-4">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={goBack}
              disabled={currentIndex === 0 || setupMutation.isPending}
            >
              <ArrowLeft className="h-3.5 w-3.5" />
              Atrás
            </Button>
            {step === "review" ? (
              <Button type="button" size="sm" onClick={() => void submit()} loading={setupMutation.isPending}>
                {setupMutation.isPending ? "Aplicando…" : "Aplicar instalación"}
                <ArrowRight className="h-3.5 w-3.5" />
              </Button>
            ) : (
              <Button type="button" size="sm" onClick={goNext}>
                Siguiente
                <ArrowRight className="h-3.5 w-3.5" />
              </Button>
            )}
          </div>
        )}
      </Surface>
    </div>
  );
}
