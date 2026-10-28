"use client";

import React from "react";
import { Check, Copy, KeyRound } from "lucide-react";
import { Input } from "@/shared/components/ui/Input";
import { Button } from "@/shared/components/ui/Button";
import { useConfigureWidget, useRotateWidgetKey, type ChannelConnection, type WebChatWidget } from "../api/useChannelsData";

/** Código que el cliente pega en su sitio. El script se sirve desde el mismo panel que habla con la API. */
function embedSnippet(publicKey: string) {
  const origin = typeof window === "undefined" ? "" : window.location.origin;
  return `<script src="${origin}/ocap-webchat.js" data-ocap-key="${publicKey}" async></script>`;
}

function parseOrigins(text: string) {
  return text
    .split(/[\s,]+/)
    .map((o) => o.trim().replace(/\/+$/, ""))
    .filter(Boolean);
}

export function WebChatWidgetForm({ channel, widget, onDone }: { channel: ChannelConnection; widget: WebChatWidget; onDone: () => void }) {
  const [title, setTitle] = React.useState(widget.title);
  const [origins, setOrigins] = React.useState(widget.allowedOrigins.join("\n"));
  const [copied, setCopied] = React.useState(false);
  const configure = useConfigureWidget();
  const rotate = useRotateWidgetKey();
  const publicKey = rotate.data?.publicKey ?? widget.publicKey;
  const snippet = embedSnippet(publicKey);
  const error = configure.error ?? rotate.error;

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(snippet);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Sin portapapeles (http sin TLS): el código queda seleccionable en el cuadro.
    }
  };

  return (
    <form
      className="space-y-5"
      onSubmit={(e) => {
        e.preventDefault();
        configure.mutate(
          { connectionId: channel.id, widgetTitle: title.trim(), allowedOrigins: parseOrigins(origins) },
          { onSuccess: onDone }
        );
      }}
    >
      <Input id={`widget-title-${channel.id}`} label="Título del chat" maxLength={60} value={title} onChange={(e) => setTitle(e.target.value)} />

      <div className="space-y-1.5">
        <label htmlFor={`widget-origins-${channel.id}`} className="block text-xs font-medium tracking-wide text-neutral-700">
          Sitios donde se puede usar
        </label>
        <textarea
          id={`widget-origins-${channel.id}`}
          aria-describedby={`widget-origins-${channel.id}-hint`}
          rows={3}
          value={origins}
          onChange={(e) => setOrigins(e.target.value)}
          placeholder="https://tutienda.com"
          className="focus-ring w-full rounded-md border border-neutral-300 bg-white px-3.5 py-2.5 font-mono text-sm text-neutral-950 placeholder:text-neutral-500"
        />
        <p id={`widget-origins-${channel.id}-hint`} className="text-xs text-neutral-600">
          Uno por línea, sin rutas. Otros sitios no podrán usar tu chat ni gastar tu IA. Vacío: ninguno.
        </p>
      </div>

      <div className="space-y-1.5">
        <p className="text-xs font-medium tracking-wide text-neutral-700">Pega esto antes de &lt;/body&gt; en tu sitio</p>
        <div className="flex items-start gap-2">
          <code className="block flex-1 select-all break-all rounded-md border border-neutral-200 bg-neutral-50 p-3 font-mono text-xs text-neutral-900">
            {snippet}
          </code>
          <Button type="button" variant="secondary" size="sm" onClick={copy} aria-label="Copiar código">
            {copied ? <Check className="h-3.5 w-3.5" aria-hidden /> : <Copy className="h-3.5 w-3.5" aria-hidden />}
            {copied ? "Copiado" : "Copiar"}
          </Button>
        </div>
        <span className="sr-only" aria-live="polite">
          {copied ? "Código copiado" : ""}
        </span>
      </div>

      <div className="flex items-start justify-between gap-3 rounded-md border border-neutral-200 p-3">
        <p className="text-xs text-neutral-600">
          ¿Alguien copió tu código en un sitio ajeno? Genera una clave nueva: la anterior deja de funcionar y tendrás que
          actualizar el código en tu sitio.
        </p>
        <Button type="button" variant="secondary" size="sm" loading={rotate.isPending} onClick={() => rotate.mutate(channel.id)}>
          <KeyRound className="h-3.5 w-3.5" aria-hidden /> Nueva clave
        </Button>
      </div>

      {error ? (
        <p role="alert" className="text-xs font-medium text-danger">
          {error instanceof Error ? error.message : "No se pudo guardar."}
        </p>
      ) : null}

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" size="sm" onClick={onDone}>
          Cerrar
        </Button>
        <Button type="submit" size="sm" loading={configure.isPending}>
          Guardar
        </Button>
      </div>
    </form>
  );
}
