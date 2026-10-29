"use client";

import React from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import { Button, CopyButton, FormError, Input } from "@/shared/components/ui";

interface CloudConnected {
  data: { verifyToken: string; webhookPath: string };
}

/**
 * WhatsApp Business Platform (API oficial de Meta). Los secretos se guardan cifrados en la conexión: no hace falta
 * tocar el .env. El token de verificación lo genera OCAP y se muestra una sola vez para pegarlo en Meta.
 */
export function WhatsAppCloudForm({ onCancel, onDone }: { onCancel: () => void; onDone: () => void }) {
  const queryClient = useQueryClient();
  const [displayName, setDisplayName] = React.useState("");
  const [phoneNumberId, setPhoneNumberId] = React.useState("");
  const [apiToken, setApiToken] = React.useState("");
  const [appSecret, setAppSecret] = React.useState("");
  const connect = useMutation({
    mutationFn: () =>
      apiClient.post<CloudConnected>("/api/channels/whatsapp/connect", {
        displayName: displayName.trim(),
        phoneNumberId: phoneNumberId.trim(),
        apiToken: apiToken.trim(),
        appSecret: appSecret.trim(),
      }),
    onSuccess: () => {
      setApiToken("");
      setAppSecret("");
      void queryClient.invalidateQueries({ queryKey: ["channels"] });
    },
  });

  if (connect.data) {
    const apiBase = process.env.NEXT_PUBLIC_API_URL || window.location.origin;
    const webhookUrl = `${apiBase.replace(/\/$/, "")}${connect.data.data.webhookPath}`;
    return (
      <div className="space-y-4">
        <p className="text-sm text-neutral-800">
          Número conectado. En Meta, en la configuración del webhook de tu app, pega estos dos datos y suscríbete a «messages».
        </p>
        <div className="space-y-1">
          <p className="text-sm font-medium text-neutral-800">URL de devolución de llamada</p>
          <div className="flex items-center gap-2">
            <code className="min-w-0 flex-1 truncate rounded-md bg-neutral-100 px-3 py-2 text-xs">{webhookUrl}</code>
            <CopyButton value={webhookUrl} />
          </div>
        </div>
        <div className="space-y-1">
          <p className="text-sm font-medium text-neutral-800">Token de verificación</p>
          <div className="flex items-center gap-2">
            <code className="min-w-0 flex-1 truncate rounded-md bg-neutral-100 px-3 py-2 text-xs">{connect.data.data.verifyToken}</code>
            <CopyButton value={connect.data.data.verifyToken} />
          </div>
          <p className="text-xs text-warning">Cópialo ahora: no se vuelve a mostrar.</p>
        </div>
        <div className="flex justify-end">
          <Button onClick={onDone}>Listo</Button>
        </div>
      </div>
    );
  }

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault();
        connect.mutate();
      }}
    >
      <Input label="Nombre" required maxLength={60} placeholder="WhatsApp tienda" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
      <Input
        label="Phone number ID"
        hint="En Meta: WhatsApp > Configuración de la API."
        required
        inputMode="numeric"
        value={phoneNumberId}
        onChange={(e) => setPhoneNumberId(e.target.value)}
      />
      <Input label="Token de acceso" type="password" autoComplete="off" required value={apiToken} onChange={(e) => setApiToken(e.target.value)} />
      <Input
        label="App Secret"
        hint="En Meta: Configuración de la app > Básica. Verifica que los mensajes vienen de Meta."
        type="password"
        autoComplete="off"
        required
        value={appSecret}
        onChange={(e) => setAppSecret(e.target.value)}
      />
      <FormError error={connect.error} />
      <div className="flex justify-end gap-2 pt-1">
        <Button variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button type="submit" loading={connect.isPending}>
          Conectar
        </Button>
      </div>
    </form>
  );
}
