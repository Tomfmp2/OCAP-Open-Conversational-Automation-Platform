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
  defaultInstallerForm,
  getInstallerSteps,
  parseGoogleCredentialsJson,
  resolveApiUrl,
  validateInstallerStep,
  type InstallerFormState,
  type InstallerStepId,
  type InstallerTarget,
} from "../model/installerForm";

function StepRail({
  steps,
  current,
}: {
  steps: { id: InstallerStepId; title: string }[];
  current: InstallerStepId;
}) {
  const currentIndex = steps.findIndex((s) => s.id === current);
  return (
    <ol className="flex flex-wrap gap-2">
      {steps.map((step, index) => {
        const done = index < currentIndex;
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
  const [setupMeta, setSetupMeta] = React.useState<{
    requiresRestart: boolean;
    adminCreated: boolean;
    adminUpdated?: boolean;
    restartHint: string;
    envFilePath?: string;
    envKeysUpdated?: string[];
  } | null>(null);
  const [forceWizard, setForceWizard] = React.useState(false);
  const [googleJsonPaste, setGoogleJsonPaste] = React.useState("");
  const [googleJsonError, setGoogleJsonError] = React.useState<string | null>(null);
  const setupMutation = useInstallerSetupMutation();

  const steps = React.useMemo(() => getInstallerSteps(form.target), [form.target]);
  const showDiagnosticOnly = Boolean(status?.completed) && !forceWizard;
  const diagnosticQuery = useInstallerDiagnostic(showDiagnosticOnly || step === "diagnostic");

  React.useEffect(() => {
    if (status?.completed && !forceWizard) {
      setStep("diagnostic");
    }
  }, [status?.completed, forceWizard]);

  const patch = <K extends keyof InstallerFormState>(key: K, value: InstallerFormState[K]) => {
    setForm((prev) => {
      const next = { ...prev, [key]: value };
      if (key === "target") {
        const t = value as InstallerTarget;
        if (t === "Dev") {
          next.apiHostPort = 5229;
          next.frontendHostPort = 3000;
        } else if (t === "Local") {
          next.apiHostPort = 5000;
          next.frontendHostPort = 3000;
        }
      }
      return next;
    });
    setError(null);
  };

  const currentIndex = steps.findIndex((s) => s.id === step);

  React.useEffect(() => {
    if (!steps.some((s) => s.id === step)) {
      setStep(steps[0]?.id ?? "mode");
    }
  }, [steps, step]);

  const goNext = () => {
    const validationError = validateInstallerStep(step, form);
    if (validationError) {
      setError(validationError);
      return;
    }
    const next = steps[currentIndex + 1];
    if (next) setStep(next.id);
  };

  const goBack = () => {
    const prev = steps[currentIndex - 1];
    if (prev) setStep(prev.id);
  };

  const submit = async () => {
    const validationError =
      validateInstallerStep("admin", form) ||
      validateInstallerStep("ai", form) ||
      validateInstallerStep("google", form) ||
      validateInstallerStep("network", form) ||
      validateInstallerStep("database", form);
    if (validationError) {
      setError(validationError);
      return;
    }

    try {
      const result = await setupMutation.mutateAsync({
        form,
        skipAuth: Boolean(status?.allowsAnonymousSetup ?? !status?.completed),
      });
      setSetupResult(result.message);
      setSetupMeta({
        requiresRestart: result.requiresRestart,
        adminCreated: result.adminCreated,
        adminUpdated: result.adminUpdated,
        restartHint: result.restartHint,
        envFilePath: result.envFilePath || result.dotEnvPath,
        envKeysUpdated: result.envKeysUpdated,
      });
      setStep("diagnostic");
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo completar la instalación.");
    }
  };

  if (statusLoading) {
    return (
      <Surface className="flex items-center gap-2 p-6 text-sm text-neutral-500">
        <Loader2 className="h-4 w-4 animate-spin" />
        Cargando estado del instalador…
      </Surface>
    );
  }

  if (showDiagnosticOnly) {
    return (
      <div className="space-y-4">
        <Surface className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-2 text-sm text-neutral-950">
            <CheckCircle2 className="h-4 w-4" />
            Instalación marcada como completa.
          </div>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => {
              setForceWizard(true);
              setStep("mode");
            }}
          >
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
        <p className="text-xs text-neutral-500">
          Para reconfigurar necesitas sesión de admin (el setup anónimo queda bloqueado tras Completed).
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <StepRail steps={steps} current={step} />

      <Surface className="space-y-5 p-5">
        {step === "mode" && (
          <div className="space-y-4">
            <h2 className="text-sm font-semibold text-neutral-950">¿Cómo vas a ejecutar OCAP?</h2>
            <div className="grid gap-3 sm:grid-cols-3">
              {(
                [
                  {
                    id: "Dev" as const,
                    title: "Dev local",
                    desc: "Sin Docker · ocap-dev · API :5229 · UseInMemory",
                  },
                  {
                    id: "Local" as const,
                    title: "Docker Local",
                    desc: "Compose · panel :3000 · API :5000",
                  },
                  {
                    id: "Web" as const,
                    title: "Web / servidor",
                    desc: "URLs públicas + Postgres propio",
                  },
                ] as const
              ).map((opt) => (
                <button
                  key={opt.id}
                  type="button"
                  onClick={() => patch("target", opt.id)}
                  className={`rounded-md border p-4 text-left text-sm transition ${
                    form.target === opt.id
                      ? "border-neutral-950 bg-neutral-950 text-white"
                      : "border-neutral-300 bg-white text-neutral-700 hover:border-neutral-500"
                  }`}
                >
                  <p className="font-semibold">{opt.title}</p>
                  <p className="mt-1 text-xs opacity-70">{opt.desc}</p>
                </button>
              ))}
            </div>
          </div>
        )}

        {step === "network" && form.target === "Local" && (
          <div className="space-y-3 text-sm text-neutral-700">
            <p className="font-semibold text-neutral-950">Docker Local — puertos fijos</p>
            <p>
              El stack se monta con <code className="text-neutral-950">./scripts/ocap-up.sh</code> en
              panel <strong className="text-neutral-950">:3000</strong> y API{" "}
              <strong className="text-neutral-950">:5000</strong>.
            </p>
            <ul className="list-disc space-y-1 pl-5 text-xs text-neutral-500">
              <li>Panel: http://localhost:3000</li>
              <li>API: http://localhost:5000</li>
              <li>Postgres Compose: ocap_db / ocap_user · puerto host 5433</li>
            </ul>
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
              hint="Se usa para CORS y redirects OAuth."
            />
          </div>
        )}

        {step === "database" && form.target === "Web" && (
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
              hint={status?.hasAdminUsers ? "Si ya hay admin (bootstrap), se actualizará email/contraseña." : undefined}
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

        {step === "ai" && (
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5 sm:col-span-2">
              <p className="text-xs font-semibold text-neutral-700">Proveedor</p>
              <div className="flex flex-wrap gap-2">
                {(["Gemini", "OpenAI", "Claude", "Ollama"] as const).map((p) => (
                  <Button
                    key={p}
                    type="button"
                    size="sm"
                    variant={form.aiProvider === p ? "primary" : "secondary"}
                    onClick={() => {
                      patch("aiProvider", p);
                      if (p === "Gemini") patch("aiModelName", "gemini-3.5-flash");
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
              hint={
                form.target === "Dev"
                  ? "Opcional en Dev: se conserva la del .env si no rellenas."
                  : form.aiProvider === "Ollama"
                    ? "Opcional para Ollama."
                    : undefined
              }
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

        {step === "google" && (
          <div className="space-y-4">
            <label className="flex items-center gap-2 text-sm text-neutral-700">
              <input
                type="checkbox"
                checked={form.enableGoogleWorkspace}
                onChange={(e) => patch("enableGoogleWorkspace", e.target.checked)}
              />
              Activar Google Workspace (Gmail / Sheets / Calendar)
            </label>
            {!form.enableGoogleWorkspace && (
              <p className="text-xs text-neutral-500">
                Sin activar, Google queda en modo in-memory (útil en Dev).
              </p>
            )}
            {form.enableGoogleWorkspace && (
              <div className="grid gap-4">
                <div className="space-y-2 rounded-md border border-neutral-200 bg-neutral-50 p-3">
                  <p className="text-xs font-semibold text-neutral-800">
                    Pegar JSON de Google (recomendado)
                  </p>
                  <p className="text-[11px] leading-relaxed text-neutral-500">
                    Google no permite obtener el Secret solo con el Client ID. Descarga el JSON
                    del cliente OAuth en Cloud Console y pégalo aquí: se rellenan ID y Secret a la vez.
                  </p>
                  <textarea
                    className="min-h-[88px] w-full rounded-md border border-neutral-300 bg-white px-3 py-2 font-mono text-[11px] text-neutral-800 outline-none focus:border-neutral-950"
                    placeholder='{"web":{"client_id":"...","client_secret":"..."}}'
                    value={googleJsonPaste}
                    onChange={(e) => {
                      const value = e.target.value;
                      setGoogleJsonPaste(value);
                      setGoogleJsonError(null);
                      const parsed = parseGoogleCredentialsJson(value);
                      if (!value.trim()) return;
                      if (!parsed) {
                        setGoogleJsonError("JSON inválido o sin client_id / client_secret.");
                        return;
                      }
                      patch("googleClientId", parsed.clientId);
                      patch("googleClientSecret", parsed.clientSecret);
                      if (parsed.redirectUri) patch("googleRedirectUri", parsed.redirectUri);
                      setGoogleJsonError(null);
                    }}
                  />
                  <label className="inline-flex cursor-pointer items-center gap-2 text-xs text-neutral-600 hover:text-neutral-950">
                    <input
                      type="file"
                      accept="application/json,.json"
                      className="sr-only"
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (!file) return;
                        void file.text().then((text) => {
                          setGoogleJsonPaste(text);
                          const parsed = parseGoogleCredentialsJson(text);
                          if (!parsed) {
                            setGoogleJsonError("El archivo no contiene client_id / client_secret.");
                            return;
                          }
                          patch("googleClientId", parsed.clientId);
                          patch("googleClientSecret", parsed.clientSecret);
                          if (parsed.redirectUri) patch("googleRedirectUri", parsed.redirectUri);
                          setGoogleJsonError(null);
                        });
                      }}
                    />
                    <span className="underline underline-offset-2">O subir archivo .json</span>
                  </label>
                  {googleJsonError && (
                    <p className="text-[11px] text-neutral-950">{googleJsonError}</p>
                  )}
                </div>
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
                  hint={`Por defecto: ${resolveApiUrl(form)}/api/integrations/Google/connect`}
                />
              </div>
            )}
          </div>
        )}

        {step === "review" && (
          <div className="space-y-3 text-sm text-neutral-700">
            <p>
              <span className="text-neutral-500">Modo:</span> {form.target}
            </p>
            <p>
              <span className="text-neutral-500">Red:</span>{" "}
              {form.target === "Dev"
                ? "panel :3000, API :5229 (sin Docker)"
                : form.target === "Local"
                  ? "panel :3000, API :5000 (Compose)"
                  : `${form.publicPanelUrl} → ${form.publicApiUrl}`}
            </p>
            <p>
              <span className="text-neutral-500">Admin:</span> {form.adminEmail} ({form.tenantSlug})
            </p>
            <p>
              <span className="text-neutral-500">IA:</span> {form.aiProvider} / {form.aiModelName}
              {!form.aiApiKey && form.target === "Dev" ? " (key desde .env si existe)" : ""}
            </p>
            <p>
              <span className="text-neutral-500">Google:</span>{" "}
              {form.enableGoogleWorkspace ? "OAuth real" : "in-memory / omitido"}
            </p>
            <p className="text-xs text-neutral-500">
              Canales (WhatsApp / Telegram) se configuran después en el panel, no en este wizard.
            </p>
          </div>
        )}

        {step === "diagnostic" && (
          <div className="space-y-4">
            {setupResult && (
              <div className="space-y-2 rounded-md border border-neutral-950 bg-neutral-100 p-3 text-xs text-neutral-800">
                <p>{setupResult}</p>
                {setupMeta && (
                  <ul className="list-disc space-y-1 pl-4 text-neutral-700">
                    <li>
                      Admin:{" "}
                      {setupMeta.adminCreated
                        ? "creado"
                        : "actualizado (usa el email/contraseña del wizard en /login)"}
                    </li>
                    <li>
                      Reinicio: {setupMeta.requiresRestart ? "recomendado" : "reinicia ocap-dev si cambiaste .env"}
                    </li>
                    {setupMeta.envKeysUpdated && setupMeta.envKeysUpdated.length > 0 && (
                      <li>
                        Claves .env tocadas: {setupMeta.envKeysUpdated.slice(0, 8).join(", ")}
                        {setupMeta.envKeysUpdated.length > 8
                          ? ` (+${setupMeta.envKeysUpdated.length - 8})`
                          : ""}
                      </li>
                    )}
                    {setupMeta.envFilePath && <li>Artefactos: {setupMeta.envFilePath}</li>}
                    <li className="font-mono text-[11px]">{setupMeta.restartHint}</li>
                  </ul>
                )}
              </div>
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
              <p className="text-sm text-neutral-500">Cargando diagnóstico…</p>
            )}
          </div>
        )}

        {error && (
          <p className="rounded-md border border-2 border-neutral-950 bg-white px-3 py-2 text-xs text-neutral-950">
            {error}
          </p>
        )}

        {step !== "diagnostic" && (
          <div className="flex items-center justify-between gap-3 border-t border-neutral-200 pt-4">
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
