import React from "react";
import { Radio, Plus, Trash2 } from "lucide-react";
import { WebhookItem } from "../api/useDeveloperData";
import {
  Badge,
  Button,
  EmptyState,
  Input,
  Modal,
  Surface,
} from "@/shared/components/ui";

interface WebhookManagerProps {
  webhooks: WebhookItem[];
  onCreate?: (input: { name: string; targetUrl: string; events: string[]; secret: string }) => void | Promise<unknown>;
  onDelete?: (id: string) => void | Promise<unknown>;
  isCreating?: boolean;
  isDeleting?: boolean;
}

export function WebhookManager({
  webhooks,
  onCreate,
  onDelete,
  isCreating = false,
  isDeleting = false,
}: WebhookManagerProps) {
  const [isOpen, setIsOpen] = React.useState(false);
  const [name, setName] = React.useState("");
  const [targetUrl, setTargetUrl] = React.useState("");
  const [events, setEvents] = React.useState("");
  const [secret, setSecret] = React.useState("");

  const handleCreate = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!onCreate || !name.trim() || !targetUrl.trim() || secret.length < 16) return;
    await onCreate({
      name: name.trim(),
      targetUrl: targetUrl.trim(),
      events: events.split(",").map((item) => item.trim()).filter(Boolean),
      secret,
    });
    setName("");
    setTargetUrl("");
    setEvents("");
    setSecret("");
    setIsOpen(false);
  };

  const statusTone = (status: WebhookItem["status"]) =>
    status === "active" ? "success" : status === "failing" ? "danger" : "neutral";

  return (
    <Surface variant="glass" glow className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800/80 pb-4">
        <div className="flex items-center gap-2">
          <div className="rounded-xl bg-violet-500/10 p-2 text-violet-400">
            <Radio className="h-4 w-4" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-zinc-100">Webhooks</h2>
            <p className="text-[11px] text-zinc-500">Entregas de eventos a servicios externos.</p>
          </div>
        </div>
        <Button variant="secondary" size="sm" onClick={() => setIsOpen(true)} disabled={!onCreate}>
          <Plus className="h-3.5 w-3.5" />
          Registrar endpoint
        </Button>
      </div>

      {webhooks.length === 0 ? (
        <EmptyState
          title="No hay webhooks registrados"
          description="No se enviarán eventos a endpoints externos."
        />
      ) : (
        <div className="space-y-3">
          {webhooks.map((webhook) => (
          <div
            key={webhook.id}
            className="rounded-xl border border-zinc-800 bg-zinc-950/70 p-4"
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="text-sm font-semibold text-zinc-100">{webhook.name}</p>
                <p className="mt-1 truncate font-mono text-xs text-zinc-400">{webhook.url}</p>
              </div>
              <div className="flex items-center gap-2">
                <Badge tone={statusTone(webhook.status)}>
                  {webhook.status === "active" ? "Activo" : webhook.status === "failing" ? "Con fallos" : "Inactivo"}
                </Badge>
                <Button
                  variant="ghost"
                  size="sm"
                  className="px-2 text-red-400"
                  disabled={!onDelete || isDeleting}
                  onClick={() => void onDelete?.(webhook.id)}
                  aria-label={`Eliminar ${webhook.name}`}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
            <div className="mt-3 flex flex-wrap gap-1.5">
              {webhook.events.map((eventName) => (
                <Badge key={eventName} tone="accent" className="font-mono normal-case">
                  {eventName}
                </Badge>
              ))}
            </div>
          </div>
          ))}
        </div>
      )}

      <Modal
        open={isOpen}
        onClose={() => setIsOpen(false)}
        title="Registrar webhook"
        description="Configura el destino y los eventos que recibirá."
      >
        <form className="space-y-4" onSubmit={handleCreate}>
          <Input label="Nombre" value={name} onChange={(event) => setName(event.target.value)} />
          <Input
            label="URL de destino"
            type="url"
            value={targetUrl}
            onChange={(event) => setTargetUrl(event.target.value)}
            placeholder="https://example.com/webhooks/ocap"
          />
          <Input
            label="Eventos"
            hint="Separados por comas. Déjalo vacío si la API admite todos los eventos."
            value={events}
            onChange={(event) => setEvents(event.target.value)}
            placeholder="agent.completed, task.failed"
          />
          <Input
            label="Secreto de firma"
            type="password"
            value={secret}
            onChange={(event) => setSecret(event.target.value)}
            hint="Se enviará a la API para firmar las entregas."
            minLength={16}
            required
          />
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" onClick={() => setIsOpen(false)}>
              Cancelar
            </Button>
            <Button
              type="submit"
              loading={isCreating}
              disabled={!name.trim() || !targetUrl.trim() || secret.length < 16}
            >
              Registrar
            </Button>
          </div>
        </form>
      </Modal>
    </Surface>
  );
}
