"use client";

import React from "react";
import { Button, FormError, Input, Modal } from "@/shared/components/ui";
import { cn } from "@/shared/utils/cn";
import { useConnectChannelMutation } from "../api/useChannelsData";
import { WhatsAppCloudForm } from "./WhatsAppCloudForm";
import { WhatsAppLinkPanel } from "./WhatsAppLinkPanel";

type Provider = "WhatsApp" | "WhatsAppCloud" | "Telegram" | "WebChat";

const PROVIDERS: Array<{ id: Provider; label: string; description: string }> = [
  { id: "WhatsApp", label: "WhatsApp", description: "Vincula un número escaneando un QR." },
  { id: "WhatsAppCloud", label: "WhatsApp Business", description: "API oficial de Meta, con tu app." },
  { id: "Telegram", label: "Telegram", description: "Conecta un bot con su token." },
  { id: "WebChat", label: "WebChat", description: "Un chat para tu sitio web." },
];

export function ChannelConnectModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [provider, setProvider] = React.useState<Provider>("WhatsApp");
  const [displayName, setDisplayName] = React.useState("");
  const [botToken, setBotToken] = React.useState("");
  const connect = useConnectChannelMutation();

  const close = () => {
    setDisplayName("");
    setBotToken("");
    connect.reset();
    onClose();
  };

  return (
    <Modal open={open} onClose={close} title="Conectar canal">
      <div className="space-y-5">
        <fieldset>
          <legend className="mb-2 text-sm font-medium text-neutral-800">Tipo de canal</legend>
          <div className="grid gap-2 sm:grid-cols-2">
            {PROVIDERS.map((option) => (
              <label
                key={option.id}
                className={cn(
                  "cursor-pointer rounded-lg border p-3 transition-colors has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-neutral-950",
                  provider === option.id ? "border-neutral-950 bg-neutral-50" : "border-neutral-200 hover:border-neutral-300"
                )}
              >
                <input
                  type="radio"
                  name="channel-provider"
                  value={option.id}
                  checked={provider === option.id}
                  onChange={() => {
                    setProvider(option.id);
                    connect.reset();
                  }}
                  className="sr-only"
                />
                <span className="block text-sm font-medium text-neutral-950">{option.label}</span>
                <span className="mt-0.5 block text-xs text-neutral-600">{option.description}</span>
              </label>
            ))}
          </div>
        </fieldset>

        {provider === "WhatsApp" ? (
          <WhatsAppLinkPanel active={open} onLinked={close} />
        ) : provider === "WhatsAppCloud" ? (
          <WhatsAppCloudForm onCancel={close} onDone={close} />
        ) : (
          <form
            className="space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              connect.mutate(
                {
                  provider,
                  displayName: displayName.trim(),
                  botToken: provider === "Telegram" ? botToken.trim() : undefined,
                  widgetTitle: provider === "WebChat" ? displayName.trim() : undefined,
                },
                { onSuccess: close }
              );
            }}
          >
            <Input
              label="Nombre"
              hint={provider === "WebChat" ? "También es el título que ven tus visitantes en el chat." : "Solo lo ves tú, en el panel."}
              required
              maxLength={60}
              placeholder={provider === "Telegram" ? "Bot de soporte" : "Atención en la web"}
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
            />
            {provider === "Telegram" && (
              <Input
                label="Token del bot"
                hint="Te lo da @BotFather en Telegram al crear el bot."
                type="password"
                required
                autoComplete="off"
                placeholder="123456789:ABC…"
                value={botToken}
                onChange={(e) => setBotToken(e.target.value)}
              />
            )}
            <FormError error={connect.error} />
            <div className="flex justify-end gap-2 pt-1">
              <Button variant="ghost" onClick={close}>
                Cancelar
              </Button>
              <Button type="submit" loading={connect.isPending}>
                Conectar
              </Button>
            </div>
          </form>
        )}
      </div>
    </Modal>
  );
}
