"use client";

import React from "react";
import { AlertCircle, CheckCircle2 } from "lucide-react";
import { Button, Input } from "@/shared/components/ui";
import { PROVIDER_MODEL_PRESETS, useAiModelActions } from "@/features/intelligence/api/useAiModels";

const PROVIDERS = ["Gemini", "OpenAI", "Claude", "Ollama"] as const;
type Provider = (typeof PROVIDERS)[number];

const KEY_HINTS: Record<Provider, string> = {
  Gemini: "Créala en Google AI Studio (aistudio.google.com).",
  OpenAI: "Créala en platform.openai.com → API keys.",
  Claude: "Créala en console.anthropic.com → API keys.",
  Ollama: "Ollama corre en tu equipo y no necesita clave.",
};

const OLLAMA_DEFAULT_URL = "http://host.docker.internal:11434";

interface AiStepProps {
  onNext: () => void;
}

/**
 * Paso 2 (opcional): conecta un proveedor de IA y lo prueba en el momento. Usa los mismos endpoints que
 * "Modelos de IA", así que todo lo que se haga aquí se puede cambiar después desde el panel.
 */
export function AiStep({ onNext }: AiStepProps) {
  const { create: createProviderMutation, setActive: selectProviderMutation, test: testProviderMutation } = useAiModelActions();
  const [provider, setProvider] = React.useState<Provider>("Gemini");
  const [model, setModel] = React.useState(PROVIDER_MODEL_PRESETS.Gemini[0]);
  const [apiKey, setApiKey] = React.useState("");
  const [baseUrl, setBaseUrl] = React.useState(OLLAMA_DEFAULT_URL);
  const [keyError, setKeyError] = React.useState<string | undefined>();
  const [result, setResult] = React.useState<{ ok: boolean; message: string } | null>(null);
  const [saving, setSaving] = React.useState(false);

  const needsKey = provider !== "Ollama";

  const chooseProvider = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const next = event.target.value as Provider;
    setProvider(next);
    setModel(PROVIDER_MODEL_PRESETS[next][0]);
    setKeyError(undefined);
    setResult(null);
  };

  const handleSave = async (event: React.FormEvent) => {
    event.preventDefault();
    setResult(null);
    if (needsKey && !apiKey.trim()) {
      setKeyError("Pega la API key del proveedor, o pulsa «Omitir por ahora».");
      return;
    }

    setSaving(true);
    try {
      const created = await createProviderMutation.mutateAsync({
        providerName: provider,
        displayName: provider,
        modelName: model,
        apiKey: needsKey ? apiKey.trim() : "",
        baseUrl: provider === "Ollama" ? baseUrl.trim() : null,
      });
      await selectProviderMutation.mutateAsync(created.id);
      const test = await testProviderMutation.mutateAsync(created.id);
      setResult({ ok: true, message: `${provider} respondió en ${Math.round(test.latencyMs)} ms con ${test.modelUsed}.` });
    } catch (error: unknown) {
      const detail = error instanceof Error && error.message ? ` (${error.message})` : "";
      setResult({
        ok: false,
        message: `No se pudo conectar con ${provider}${detail}. Revisa la clave o el modelo; también puedes configurarlo después en «Modelos de IA».`,
      });
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={handleSave} noValidate className="space-y-5">
      <div className="grid gap-5 sm:grid-cols-2">
        <div className="space-y-1.5">
          <label htmlFor="aiProvider" className="block text-xs font-medium tracking-wide text-neutral-700">
            Proveedor
          </label>
          <select
            id="aiProvider"
            value={provider}
            onChange={chooseProvider}
            className="w-full rounded-md border border-neutral-300 bg-white px-3.5 py-2.5 text-sm focus-ring"
          >
            {PROVIDERS.map((name) => (
              <option key={name} value={name}>{name}</option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <label htmlFor="aiModel" className="block text-xs font-medium tracking-wide text-neutral-700">
            Modelo
          </label>
          <select
            id="aiModel"
            value={model}
            onChange={(event) => setModel(event.target.value)}
            className="w-full rounded-md border border-neutral-300 bg-white px-3.5 py-2.5 text-sm focus-ring"
          >
            {PROVIDER_MODEL_PRESETS[provider].map((name) => (
              <option key={name} value={name}>{name}</option>
            ))}
          </select>
        </div>
      </div>

      {needsKey ? (
        <Input
          id="aiApiKey"
          label="API key"
          type="password"
          autoComplete="off"
          spellCheck={false}
          value={apiKey}
          onChange={(event) => {
            setApiKey(event.target.value);
            setKeyError(undefined);
          }}
          error={keyError}
          hint={`${KEY_HINTS[provider]} Se guarda cifrada en la base de datos.`}
        />
      ) : (
        <Input
          id="aiBaseUrl"
          label="URL de Ollama"
          value={baseUrl}
          onChange={(event) => setBaseUrl(event.target.value)}
          hint={KEY_HINTS.Ollama}
        />
      )}

      {result && (
        <div
          role={result.ok ? "status" : "alert"}
          className="flex items-start gap-2 rounded-md border border-neutral-300 bg-white px-3.5 py-3 text-sm"
        >
          {result.ok ? (
            <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
          ) : (
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
          )}
          <span>{result.message}</span>
        </div>
      )}

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between">
        <Button type="button" variant="ghost" onClick={onNext}>
          {result?.ok ? "Continuar" : "Omitir por ahora"}
        </Button>
        {!result?.ok && (
          <Button type="submit" loading={saving}>
            {saving ? "Probando conexión…" : "Guardar y probar"}
          </Button>
        )}
      </div>
    </form>
  );
}
