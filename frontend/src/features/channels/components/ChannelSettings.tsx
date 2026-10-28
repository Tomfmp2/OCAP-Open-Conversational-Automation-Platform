"use client";

import React from "react";
import { Button, FormError, Input, Modal } from "@/shared/components/ui";
import { useConfigureWidget, useDeleteChannel, useRenameChannel, type ChannelConnection } from "../api/useChannelsData";
import { ChannelAgentSelect } from "./ChannelAgentSelect";
import { ChannelAiSelect } from "./ChannelAiSelect";
import { WebChatWidgetForm } from "./WebChatWidgetForm";
import { WhatsAppLinkPanel } from "./WhatsAppLinkPanel";

/** Qué pasa al eliminar, según el canal: hay que saberlo antes de confirmar. */
function deleteConsequence(channel: ChannelConnection) {
  switch (channel.provider.toLowerCase()) {
    case "whatsapp":
      return "Se cierra la sesión de WhatsApp en tu teléfono y el agente deja de responder ese número. Para volver a usarlo tendrás que escanear el QR otra vez.";
    case "telegram":
      return "El bot deja de recibir mensajes en OCAP. Sigue existiendo en Telegram: puedes conectarlo de nuevo con su token.";
    default:
      return "El chat deja de funcionar en los sitios donde lo pegaste.";
  }
}

/** Todo lo que se configura de un canal, en un solo panel. */
export function ChannelSettings({ channel, open, onClose }: { channel: ChannelConnection; open: boolean; onClose: () => void }) {
  const [confirmDelete, setConfirmDelete] = React.useState(false);
  const close = () => {
    setConfirmDelete(false);
    onClose();
  };

  return (
    <Modal open={open} onClose={close} title={confirmDelete ? `¿Eliminar ${channel.name}?` : channel.name} description={confirmDelete ? undefined : channel.provider}>
      {confirmDelete ? (
        <DeleteConfirm channel={channel} onCancel={() => setConfirmDelete(false)} onDone={close} />
      ) : (
        <div className="space-y-8">
          <RenameForm channel={channel} />

          {channel.provider.toLowerCase() !== "webchat" && (
            <Group title="Quién responde">
              <ChannelAgentSelect connectionId={channel.id} agentId={channel.agentId} />
              <ChannelAiSelect connectionId={channel.id} aiProviderConfigId={channel.aiProviderConfigId} />
            </Group>
          )}

          {channel.widget && (
            <Group title="Chat en tu sitio web">
              <WebChatWidgetForm channel={channel} widget={channel.widget} />
            </Group>
          )}

          {channel.provider.toLowerCase() === "whatsapp" && (
            <Group title="Teléfono vinculado">
              <WhatsAppLinkPanel active={open} />
            </Group>
          )}

          <div className="flex justify-between gap-3 border-t border-neutral-200 pt-5">
            <Button variant="ghost" className="text-danger hover:bg-danger-subtle hover:text-danger" onClick={() => setConfirmDelete(true)}>
              Eliminar canal
            </Button>
            <Button onClick={close}>Listo</Button>
          </div>
        </div>
      )}
    </Modal>
  );
}

function Group({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="space-y-4">
      <h3 className="text-sm font-semibold text-neutral-950">{title}</h3>
      {children}
    </section>
  );
}

function RenameForm({ channel }: { channel: ChannelConnection }) {
  const [name, setName] = React.useState(channel.name);
  const rename = useRenameChannel();
  const configureWidget = useConfigureWidget();
  const unchanged = name.trim() === channel.name;

  return (
    <form
      className="flex items-end gap-2"
      onSubmit={(e) => {
        e.preventDefault();
        const displayName = name.trim();
        rename.mutate(
          { connectionId: channel.id, displayName },
          {
            // En un WebChat el nombre también es el título que ven los visitantes.
            onSuccess: () =>
              channel.widget &&
              configureWidget.mutate({ connectionId: channel.id, widgetTitle: displayName, allowedOrigins: channel.widget.allowedOrigins }),
          }
        );
      }}
    >
      <div className="min-w-0 flex-1">
        <Input label="Nombre" required maxLength={100} value={name} onChange={(e) => setName(e.target.value)} />
      </div>
      <Button type="submit" variant="secondary" loading={rename.isPending} disabled={unchanged || !name.trim()}>
        Guardar
      </Button>
      <FormError error={rename.error ?? configureWidget.error} />
    </form>
  );
}

function DeleteConfirm({ channel, onCancel, onDone }: { channel: ChannelConnection; onCancel: () => void; onDone: () => void }) {
  const remove = useDeleteChannel();

  return (
    <div className="space-y-5">
      <p className="text-sm text-neutral-700">{deleteConsequence(channel)}</p>
      <p className="text-sm text-neutral-600">Las conversaciones que ya llegaron se conservan.</p>
      <FormError error={remove.error} />
      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button variant="danger" loading={remove.isPending} onClick={() => remove.mutate(channel.id, { onSuccess: onDone })}>
          Eliminar canal
        </Button>
      </div>
    </div>
  );
}
