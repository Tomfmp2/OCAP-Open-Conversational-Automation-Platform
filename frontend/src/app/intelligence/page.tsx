"use client";

import React from "react";
import { Plus } from "lucide-react";
import {
  OLLAMA_DEFAULT_URL,
  OPENAI_COMPATIBLE_SERVICES,
  PROVIDER_LABELS,
  PROVIDER_MODEL_PRESETS,
  useAiModelActions,
  useAiModels,
  useProviderModels,
  usesBaseUrl,
  type AiModel,
  type ProviderName,
} from "@/features/intelligence/api/useAiModels";
import { Badge, Button, EmptyState, ErrorState, FormError, Input, ListSkeleton, Modal, Page, PageHeader, Select } from "@/shared/components/ui";

export default function AiModelsPage() {
  const models = useAiModels();
  const [adding, setAdding] = React.useState(false);

  return (
    <Page>
      <PageHeader
        title="Modelos de IA"
        description="La IA con la que responden tus agentes. Las claves se guardan cifradas."
        actions={
          <Button onClick={() => setAdding(true)}>
            <Plus className="h-4 w-4" aria-hidden /> Agregar modelo
          </Button>
        }
      />

      {models.isLoading ? (
        <ListSkeleton label="Cargando modelos" />
      ) : models.isError ? (
        <ErrorState message={models.error instanceof Error ? models.error.message : undefined} onRetry={() => void models.refetch()} />
      ) : !models.data?.length ? (
        <EmptyState
          title="Sin modelos de IA"
          description="Mientras no agregues uno, los agentes responden en modo demostración. Necesitas una API key de Gemini, OpenAI, Claude o un servicio compatible (NVIDIA, Groq, OpenRouter), u Ollama en tu equipo."
          action={<Button onClick={() => setAdding(true)}>Agregar modelo</Button>}
        />
      ) : (
        <ul className="divide-y divide-neutral-200 rounded-lg border border-neutral-200 bg-white">
          {models.data.map((model) => (
            <ModelRow key={model.id} model={model} />
          ))}
        </ul>
      )}

      <AddModel open={adding} onClose={() => setAdding(false)} existingNames={models.data?.map((m) => m.displayName) ?? []} />
    </Page>
  );
}

function ModelRow({ model }: { model: AiModel }) {
  const [editing, setEditing] = React.useState(false);
  const { test } = useAiModelActions();

  return (
    <li className="space-y-2 px-4 py-3 sm:px-5">
      <div className="flex flex-wrap items-center gap-3">
        <div className="min-w-0 flex-1">
          <p className="flex flex-wrap items-center gap-2">
            <span className="truncate text-sm font-medium text-neutral-950">{model.displayName}</span>
            {model.isActive && model.isEnabled && <Badge tone="success">Predeterminado</Badge>}
            {!model.isEnabled && <Badge>Desactivado</Badge>}
          </p>
          <p className="truncate text-sm text-neutral-600">
            {PROVIDER_LABELS[model.providerName] ?? model.providerName}, {model.modelName}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" size="sm" loading={test.isPending} disabled={!model.isEnabled} onClick={() => test.mutate(model.id)}>
            Probar
          </Button>
          <Button variant="secondary" size="sm" onClick={() => setEditing(true)} aria-label={`Editar ${model.displayName}`}>
            Editar
          </Button>
        </div>
      </div>
      {test.isSuccess && (
        <p role="status" className="text-sm text-success">
          Responde bien ({Math.round(test.data.latencyMs)} ms).
        </p>
      )}
      <FormError error={test.error} />
      <EditModel model={model} open={editing} onClose={() => setEditing(false)} />
    </li>
  );
}

/** Campo de modelo con sugerencias; «Ver modelos disponibles» las reemplaza por la lista real del proveedor. */
function ModelField({
  provider,
  model,
  setModel,
  canList,
  list,
}: {
  provider: ProviderName;
  model: string;
  setModel: (v: string) => void;
  canList: boolean;
  list: () => Promise<string[]>;
}) {
  const listId = React.useId();
  const [available, setAvailable] = React.useState<string[] | null>(null);
  const [state, setState] = React.useState<"idle" | "loading" | "error">("idle");
  const options = available ?? PROVIDER_MODEL_PRESETS[provider];

  const load = async () => {
    setState("loading");
    try {
      setAvailable(await list());
      setState("idle");
    } catch {
      setState("error");
    }
  };

  return (
    <div className="space-y-2">
      <Input
        label="Modelo"
        required
        list={listId}
        value={model}
        onChange={(e) => setModel(e.target.value)}
        hint={available ? `${available.length} modelos disponibles. Elige uno o escribe otro.` : "Elige uno de la lista o escribe otro."}
      />
      <datalist id={listId}>
        {options.map((option) => (
          <option key={option} value={option} />
        ))}
      </datalist>
      <Button variant="secondary" size="sm" loading={state === "loading"} disabled={!canList} onClick={() => void load()}>
        Ver modelos disponibles
      </Button>
      {state === "error" && (
        <p role="alert" className="text-sm text-danger">
          No se pudo consultar la lista. Revisa la clave y la dirección; puedes escribir el modelo a mano.
        </p>
      )}
      {available?.length === 0 && state === "idle" && (
        <p role="status" className="text-sm text-neutral-600">
          El proveedor no devolvió modelos con esa clave. Escribe el nombre a mano.
        </p>
      )}
    </div>
  );
}

function BaseUrlField({ provider, baseUrl, setBaseUrl }: { provider: ProviderName; baseUrl: string; setBaseUrl: (v: string) => void }) {
  if (provider === "Ollama") {
    return <Input label="Dirección de Ollama" required value={baseUrl} onChange={(e) => setBaseUrl(e.target.value)} />;
  }
  const service = OPENAI_COMPATIBLE_SERVICES.find((s) => s.baseUrl === baseUrl)?.name ?? "";
  return (
    <>
      <Select
        label="Servicio"
        value={service}
        onChange={(e) => setBaseUrl(OPENAI_COMPATIBLE_SERVICES.find((s) => s.name === e.target.value)?.baseUrl ?? "")}
      >
        {OPENAI_COMPATIBLE_SERVICES.map((s) => (
          <option key={s.name} value={s.name}>
            {s.name}
          </option>
        ))}
        <option value="">Otro</option>
      </Select>
      <Input
        label="Dirección de la API"
        type="url"
        required
        placeholder="https://…/v1"
        hint="La que termina en /v1. Debe ser pública (https)."
        value={baseUrl}
        onChange={(e) => setBaseUrl(e.target.value)}
      />
    </>
  );
}

function AddModel({ open, onClose, existingNames }: { open: boolean; onClose: () => void; existingNames: string[] }) {
  const { create } = useAiModelActions();
  const listModels = useProviderModels();
  const [provider, setProvider] = React.useState<ProviderName>("Gemini");
  const [name, setName] = React.useState("");
  const [model, setModel] = React.useState(PROVIDER_MODEL_PRESETS.Gemini[0]);
  const [apiKey, setApiKey] = React.useState("");
  const [baseUrl, setBaseUrl] = React.useState(OLLAMA_DEFAULT_URL);

  // Nombre sugerido que no choque con los existentes (es único por organización).
  const suggestedName = (next: ProviderName, url: string) => {
    const base = next === "OpenAICompatible" ? (OPENAI_COMPATIBLE_SERVICES.find((s) => s.baseUrl === url)?.name ?? "Compatible") : PROVIDER_LABELS[next];
    let candidate = base;
    for (let i = 2; existingNames.includes(candidate); i++) candidate = `${base} ${i}`;
    return candidate;
  };

  const chooseProvider = (next: ProviderName) => {
    const url = next === "Ollama" ? OLLAMA_DEFAULT_URL : next === "OpenAICompatible" ? OPENAI_COMPATIBLE_SERVICES[0].baseUrl : "";
    setProvider(next);
    setBaseUrl(url);
    setModel(PROVIDER_MODEL_PRESETS[next][0] ?? "");
    setName(suggestedName(next, url));
  };

  const close = () => {
    setApiKey("");
    setName("");
    create.reset();
    onClose();
  };

  return (
    <Modal open={open} onClose={close} title="Agregar modelo de IA">
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          create.mutate(
            {
              providerName: provider,
              displayName: (name || suggestedName(provider, baseUrl)).trim(),
              modelName: model.trim(),
              apiKey: provider === "Ollama" ? "" : apiKey.trim(),
              baseUrl: usesBaseUrl(provider) ? baseUrl.trim() : null,
            },
            { onSuccess: close }
          );
        }}
      >
        <Select label="Proveedor" value={provider} onChange={(e) => chooseProvider(e.target.value as ProviderName)}>
          {(Object.keys(PROVIDER_LABELS) as ProviderName[]).map((p) => (
            <option key={p} value={p}>
              {PROVIDER_LABELS[p]}
            </option>
          ))}
        </Select>
        {usesBaseUrl(provider) && (
          <BaseUrlField
            provider={provider}
            baseUrl={baseUrl}
            setBaseUrl={(url) => {
              setBaseUrl(url);
              if (provider === "OpenAICompatible") setName(suggestedName(provider, url));
            }}
          />
        )}
        {provider !== "Ollama" && (
          <Input label="API key" type="password" autoComplete="off" required value={apiKey} onChange={(e) => setApiKey(e.target.value)} />
        )}
        <ModelField
          key={provider}
          provider={provider}
          model={model}
          setModel={setModel}
          canList={provider === "Ollama" ? !!baseUrl.trim() : !!apiKey.trim() && (provider !== "OpenAICompatible" || !!baseUrl.trim())}
          list={() =>
            listModels.mutateAsync({
              providerName: provider,
              apiKey: apiKey.trim(),
              baseUrl: usesBaseUrl(provider) ? baseUrl.trim() : null,
            })
          }
        />
        <Input
          label="Nombre"
          required
          maxLength={100}
          hint="Para distinguirlo si tienes varios del mismo proveedor."
          value={name || suggestedName(provider, baseUrl)}
          onChange={(e) => setName(e.target.value)}
        />
        <FormError error={create.error} />
        <div className="flex justify-end gap-2 pt-1">
          <Button variant="ghost" onClick={close}>
            Cancelar
          </Button>
          <Button type="submit" loading={create.isPending}>
            Agregar
          </Button>
        </div>
      </form>
    </Modal>
  );
}

function EditModel({ model, open, onClose }: { model: AiModel; open: boolean; onClose: () => void }) {
  const { update, setEnabled, setActive, remove } = useAiModelActions();
  const listModels = useProviderModels();
  const [name, setName] = React.useState(model.modelName);
  const [apiKey, setApiKey] = React.useState("");
  const [baseUrl, setBaseUrl] = React.useState(model.baseUrl ?? (model.providerName === "Ollama" ? OLLAMA_DEFAULT_URL : ""));
  const [confirmDelete, setConfirmDelete] = React.useState(false);
  const error = update.error ?? setEnabled.error ?? setActive.error ?? remove.error;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={confirmDelete ? `¿Eliminar ${model.displayName}?` : model.displayName}
      description={confirmDelete ? undefined : PROVIDER_LABELS[model.providerName] ?? model.providerName}
    >
      {confirmDelete ? (
        <div className="space-y-5">
          <p className="text-sm text-neutral-700">Se borra su clave. Los canales que lo usaban pasan al modelo predeterminado.</p>
          <FormError error={remove.error} />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={() => setConfirmDelete(false)}>
              Cancelar
            </Button>
            <Button variant="danger" loading={remove.isPending} onClick={() => remove.mutate(model.id, { onSuccess: onClose })}>
              Eliminar
            </Button>
          </div>
        </div>
      ) : (
        <form
          className="space-y-5"
          onSubmit={(e) => {
            e.preventDefault();
            update.mutate(
              { id: model.id, modelName: name.trim(), apiKey: apiKey.trim() || null, baseUrl: usesBaseUrl(model.providerName) ? baseUrl.trim() : null },
              { onSuccess: onClose }
            );
          }}
        >
          {usesBaseUrl(model.providerName) && <BaseUrlField provider={model.providerName} baseUrl={baseUrl} setBaseUrl={setBaseUrl} />}
          {model.providerName !== "Ollama" && (
            <Input
              label="API key"
              type="password"
              autoComplete="off"
              placeholder="Sin cambios"
              hint="Déjala vacía para conservar la actual."
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
            />
          )}
          {/* Con la clave guardada: la lista sale de la configuración tal como está ahora. */}
          <ModelField
            provider={model.providerName}
            model={name}
            setModel={setName}
            canList={model.isEnabled}
            list={() => listModels.mutateAsync({ configurationId: model.id })}
          />

          <div className="divide-y divide-neutral-200 border-y border-neutral-200">
            {!model.isActive && model.isEnabled && (
              <div className="flex items-center gap-4 py-3">
                <p className="min-w-0 flex-1 text-sm text-neutral-700">Usarlo en los canales que no eligen otro.</p>
                <Button variant="secondary" size="sm" loading={setActive.isPending} onClick={() => setActive.mutate(model.id)}>
                  Hacer predeterminado
                </Button>
              </div>
            )}
            <div className="flex items-center gap-4 py-3">
              <p className="min-w-0 flex-1 text-sm text-neutral-700">
                {model.isEnabled ? "Desactivarlo sin borrar su clave." : "Volver a usarlo."}
              </p>
              <Button variant="secondary" size="sm" loading={setEnabled.isPending} onClick={() => setEnabled.mutate({ id: model.id, enable: !model.isEnabled })}>
                {model.isEnabled ? "Desactivar" : "Activar"}
              </Button>
            </div>
          </div>

          <FormError error={error} />
          <div className="flex items-center justify-between gap-2">
            <Button variant="ghost" className="text-danger hover:bg-danger-subtle hover:text-danger" onClick={() => setConfirmDelete(true)}>
              Eliminar
            </Button>
            <div className="flex gap-2">
              <Button variant="ghost" onClick={onClose}>
                Cancelar
              </Button>
              <Button type="submit" loading={update.isPending}>
                Guardar
              </Button>
            </div>
          </div>
        </form>
      )}
    </Modal>
  );
}
