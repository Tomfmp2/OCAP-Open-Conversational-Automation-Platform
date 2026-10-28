"use client";

import React from "react";
import { Plus } from "lucide-react";
import {
  OLLAMA_DEFAULT_URL,
  PROVIDER_MODEL_PRESETS,
  useAiModelActions,
  useAiModels,
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
          description="Mientras no agregues uno, los agentes responden en modo demostración. Necesitas una API key de Gemini, OpenAI o Claude, u Ollama en tu equipo."
          action={<Button onClick={() => setAdding(true)}>Agregar modelo</Button>}
        />
      ) : (
        <ul className="divide-y divide-neutral-200 rounded-lg border border-neutral-200 bg-white">
          {models.data.map((model) => (
            <ModelRow key={model.id} model={model} />
          ))}
        </ul>
      )}

      <AddModel open={adding} onClose={() => setAdding(false)} />
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
            {model.providerName}, {model.modelName}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" size="sm" loading={test.isPending} disabled={!model.isEnabled} onClick={() => test.mutate(model.providerName)}>
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

function ProviderFields({
  provider,
  model,
  setModel,
  apiKey,
  setApiKey,
  baseUrl,
  setBaseUrl,
  editing,
}: {
  provider: ProviderName;
  model: string;
  setModel: (v: string) => void;
  apiKey: string;
  setApiKey: (v: string) => void;
  baseUrl: string;
  setBaseUrl: (v: string) => void;
  editing?: boolean;
}) {
  const listId = React.useId();
  return (
    <>
      <Input label="Modelo" required list={listId} value={model} onChange={(e) => setModel(e.target.value)} hint="Elige uno de la lista o escribe otro." />
      <datalist id={listId}>
        {PROVIDER_MODEL_PRESETS[provider].map((preset) => (
          <option key={preset} value={preset} />
        ))}
      </datalist>
      {provider === "Ollama" ? (
        <Input label="Dirección de Ollama" required value={baseUrl} onChange={(e) => setBaseUrl(e.target.value)} />
      ) : (
        <Input
          label="API key"
          type="password"
          autoComplete="off"
          required={!editing}
          placeholder={editing ? "Sin cambios" : undefined}
          hint={editing ? "Déjala vacía para conservar la actual." : undefined}
          value={apiKey}
          onChange={(e) => setApiKey(e.target.value)}
        />
      )}
    </>
  );
}

function AddModel({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { create } = useAiModelActions();
  const [provider, setProvider] = React.useState<ProviderName>("Gemini");
  const [model, setModel] = React.useState(PROVIDER_MODEL_PRESETS.Gemini[0]);
  const [apiKey, setApiKey] = React.useState("");
  const [baseUrl, setBaseUrl] = React.useState(OLLAMA_DEFAULT_URL);

  const close = () => {
    setApiKey("");
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
            { providerName: provider, displayName: provider, modelName: model.trim(), apiKey: provider === "Ollama" ? "" : apiKey.trim(), baseUrl: provider === "Ollama" ? baseUrl.trim() : null },
            { onSuccess: close }
          );
        }}
      >
        <Select
          label="Proveedor"
          value={provider}
          onChange={(e) => {
            const next = e.target.value as ProviderName;
            setProvider(next);
            setModel(PROVIDER_MODEL_PRESETS[next][0]);
          }}
        >
          {(Object.keys(PROVIDER_MODEL_PRESETS) as ProviderName[]).map((name) => (
            <option key={name}>{name}</option>
          ))}
        </Select>
        <ProviderFields provider={provider} model={model} setModel={setModel} apiKey={apiKey} setApiKey={setApiKey} baseUrl={baseUrl} setBaseUrl={setBaseUrl} />
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
  const [name, setName] = React.useState(model.modelName);
  const [apiKey, setApiKey] = React.useState("");
  const [baseUrl, setBaseUrl] = React.useState(model.baseUrl ?? OLLAMA_DEFAULT_URL);
  const [confirmDelete, setConfirmDelete] = React.useState(false);
  const error = update.error ?? setEnabled.error ?? setActive.error ?? remove.error;

  return (
    <Modal open={open} onClose={onClose} title={confirmDelete ? `¿Eliminar ${model.displayName}?` : model.displayName} description={confirmDelete ? undefined : model.providerName}>
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
              { id: model.id, modelName: name.trim(), apiKey: apiKey.trim() || null, baseUrl: model.providerName === "Ollama" ? baseUrl.trim() : null },
              { onSuccess: onClose }
            );
          }}
        >
          <ProviderFields provider={model.providerName} model={name} setModel={setName} apiKey={apiKey} setApiKey={setApiKey} baseUrl={baseUrl} setBaseUrl={setBaseUrl} editing />

          <div className="divide-y divide-neutral-200 border-y border-neutral-200">
            {!model.isActive && model.isEnabled && (
              <div className="flex items-center gap-4 py-3">
                <p className="min-w-0 flex-1 text-sm text-neutral-700">Usarlo en los canales que no eligen otro.</p>
                <Button variant="secondary" size="sm" loading={setActive.isPending} onClick={() => setActive.mutate(model.providerName)}>
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
