"use client";

import React from "react";
import { CheckCircle2 } from "lucide-react";
import { Button, CopyButton, FormError, Textarea } from "@/shared/components/ui";
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

/** Dónde puede usarse el chat y el código para pegar en el sitio. El título es el nombre del canal. */
export function WebChatWidgetForm({ channel, widget }: { channel: ChannelConnection; widget: WebChatWidget }) {
  const [origins, setOrigins] = React.useState(widget.allowedOrigins.join("\n"));
  const configure = useConfigureWidget();
  const rotate = useRotateWidgetKey();
  const snippet = embedSnippet(rotate.data?.publicKey ?? widget.publicKey);
  const unchanged = parseOrigins(origins).join("\n") === widget.allowedOrigins.join("\n");

  return (
    <div className="space-y-6">
      <form
        className="space-y-3"
        onSubmit={(e) => {
          e.preventDefault();
          configure.mutate({ connectionId: channel.id, widgetTitle: channel.name, allowedOrigins: parseOrigins(origins) });
        }}
      >
        <Textarea
          label="Sitios donde se puede usar"
          hint="Uno por línea, sin rutas (https://tutienda.com). Otros sitios no podrán usar tu chat."
          rows={3}
          placeholder="https://tutienda.com"
          value={origins}
          onChange={(e) => setOrigins(e.target.value)}
          className="font-mono"
        />
        <FormError error={configure.error} />
        <div className="flex items-center gap-3">
          <Button type="submit" variant="secondary" loading={configure.isPending} disabled={unchanged}>
            Guardar sitios
          </Button>
          {configure.isSuccess && unchanged && (
            <span role="status" className="flex items-center gap-1.5 text-sm text-success">
              <CheckCircle2 className="h-4 w-4" aria-hidden /> Guardado
            </span>
          )}
        </div>
      </form>

      <div className="space-y-2">
        <p className="text-sm font-medium text-neutral-800">Pega esto antes de &lt;/body&gt; en tu sitio</p>
        <code className="block select-all break-all rounded-md border border-neutral-200 bg-neutral-50 p-3 font-mono text-xs text-neutral-900">
          {snippet}
        </code>
        <CopyButton value={snippet} label="Copiar código" />
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-neutral-200 p-3">
        <p className="min-w-0 flex-1 text-sm text-neutral-600">
          Si alguien copió tu código, genera una clave nueva. Tendrás que actualizar el código en tu sitio.
        </p>
        <Button variant="secondary" size="sm" loading={rotate.isPending} onClick={() => rotate.mutate(channel.id)}>
          Generar clave nueva
        </Button>
      </div>
      <FormError error={rotate.error} />
    </div>
  );
}
