"use client";

import React from "react";
import { AlertCircle, Code2, Pencil, Trash2 } from "lucide-react";
import { Modal } from "@/shared/components/ui/Modal";
import { Input } from "@/shared/components/ui/Input";
import { Button } from "@/shared/components/ui/Button";
import { useDeleteChannel, useRenameChannel, type ChannelConnection } from "../api/useChannelsData";
import { WebChatWidgetForm } from "./WebChatWidgetForm";

/** Qué pasa al eliminar, según el canal: el usuario debe saberlo antes de confirmar. */
function deleteConsequence(channel: ChannelConnection) {
  switch (channel.provider.toLowerCase()) {
    case "whatsapp":
      return "Se cerrará la sesión de WhatsApp en tu teléfono y el asistente dejará de responder ese número. Para volver a usarlo tendrás que escanear el QR otra vez.";
    case "telegram":
      return "El bot dejará de recibir mensajes en OCAP. El bot sigue existiendo en Telegram; puedes conectarlo de nuevo con su token.";
    default:
      return "El canal dejará de funcionar en OCAP.";
  }
}

export function ChannelCardActions({ channel }: { channel: ChannelConnection }) {
  const [mode, setMode] = React.useState<"rename" | "delete" | "widget" | null>(null);

  return (
    <>
      <div className="flex items-center gap-1">
        {channel.widget && (
          <button
            type="button"
            onClick={() => setMode("widget")}
            aria-label={`Insertar ${channel.name} en tu sitio`}
            title="Insertar en tu sitio"
            className="focus-ring rounded-md p-1.5 text-neutral-600 transition-colors hover:bg-neutral-100 hover:text-neutral-950"
          >
            <Code2 className="h-3.5 w-3.5" aria-hidden />
          </button>
        )}
        <button
          type="button"
          onClick={() => setMode("rename")}
          aria-label={`Editar ${channel.name}`}
          title="Editar nombre"
          className="focus-ring rounded-md p-1.5 text-neutral-600 transition-colors hover:bg-neutral-100 hover:text-neutral-950"
        >
          <Pencil className="h-3.5 w-3.5" aria-hidden />
        </button>
        <button
          type="button"
          onClick={() => setMode("delete")}
          aria-label={`Eliminar ${channel.name}`}
          title="Eliminar canal"
          className="focus-ring rounded-md p-1.5 text-neutral-600 transition-colors hover:bg-neutral-100 hover:text-neutral-950"
        >
          <Trash2 className="h-3.5 w-3.5" aria-hidden />
        </button>
      </div>

      <Modal open={mode === "rename"} onClose={() => setMode(null)} title="Editar canal" description={channel.provider}>
        {mode === "rename" && <RenameForm channel={channel} onDone={() => setMode(null)} />}
      </Modal>
      <Modal
        open={mode === "widget"}
        onClose={() => setMode(null)}
        title="Chat en tu sitio web"
        description="Tus visitantes conversan con el agente de este canal desde tu página."
      >
        {mode === "widget" && channel.widget && (
          <WebChatWidgetForm channel={channel} widget={channel.widget} onDone={() => setMode(null)} />
        )}
      </Modal>
      <Modal open={mode === "delete"} onClose={() => setMode(null)} title={`¿Eliminar ${channel.name}?`}>
        {mode === "delete" && <DeleteConfirm channel={channel} onDone={() => setMode(null)} />}
      </Modal>
    </>
  );
}

function ErrorLine({ error }: { error: unknown }) {
  if (!error) return null;
  return (
    <p role="alert" className="flex items-center gap-2 text-xs text-danger">
      <AlertCircle className="h-3.5 w-3.5 shrink-0" aria-hidden /> {error instanceof Error ? error.message : "No se pudo completar la acción."}
    </p>
  );
}

function RenameForm({ channel, onDone }: { channel: ChannelConnection; onDone: () => void }) {
  const [name, setName] = React.useState(channel.name);
  const rename = useRenameChannel();

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault();
        rename.mutate({ connectionId: channel.id, displayName: name.trim() }, { onSuccess: onDone });
      }}
    >
      <Input id={`rename-${channel.id}`} label="Nombre" required maxLength={100} value={name} onChange={(e) => setName(e.target.value)} />
      <ErrorLine error={rename.error} />
      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" size="sm" onClick={onDone}>
          Cancelar
        </Button>
        <Button type="submit" size="sm" loading={rename.isPending} disabled={!name.trim()}>
          Guardar
        </Button>
      </div>
    </form>
  );
}

function DeleteConfirm({ channel, onDone }: { channel: ChannelConnection; onDone: () => void }) {
  const remove = useDeleteChannel();

  return (
    <div className="space-y-4">
      <p className="text-sm text-neutral-700">{deleteConsequence(channel)}</p>
      <p className="text-xs text-neutral-600">Las conversaciones ya recibidas se conservan en la bandeja.</p>
      <ErrorLine error={remove.error} />
      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" size="sm" onClick={onDone}>
          Cancelar
        </Button>
        <Button type="button" variant="danger" size="sm" loading={remove.isPending} onClick={() => remove.mutate(channel.id, { onSuccess: onDone })}>
          <Trash2 className="h-3.5 w-3.5" aria-hidden /> Eliminar canal
        </Button>
      </div>
    </div>
  );
}
