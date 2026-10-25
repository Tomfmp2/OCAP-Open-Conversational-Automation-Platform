"use client";

import React from "react";
import { Check, Copy } from "lucide-react";
import { Button } from "@/shared/components/ui/Button";
import { absoluteLink, type PasswordLink } from "../api/useTeam";

/** Muestra el enlace de un solo uso para que el administrador lo copie y lo comparta. */
export function PasswordLinkPanel({ link, recipient }: { link: PasswordLink; recipient: string }) {
  const [copied, setCopied] = React.useState(false);
  const url = absoluteLink(link);
  const expires = new Date(link.expiresAtUtc).toLocaleString();

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  };

  return (
    <div className="space-y-3">
      <p className="text-sm text-neutral-700">
        {link.emailed
          ? `Enviamos el enlace a ${recipient}. También puedes copiarlo y compartirlo por otro medio.`
          : `Comparte este enlace con ${recipient} por un medio privado. Con él define su contraseña.`}
      </p>
      <div className="flex items-stretch gap-2">
        <input
          readOnly
          value={url}
          aria-label="Enlace para definir la contraseña"
          onFocus={(e) => e.currentTarget.select()}
          className="focus-ring min-w-0 flex-1 rounded-md border border-neutral-300 bg-neutral-50 px-3 py-2 font-mono text-xs text-neutral-800"
        />
        <Button type="button" variant="secondary" size="sm" onClick={() => void copy()}>
          {copied ? <Check className="h-3.5 w-3.5" aria-hidden /> : <Copy className="h-3.5 w-3.5" aria-hidden />}
          {copied ? "Copiado" : "Copiar"}
        </Button>
      </div>
      <p className="text-xs text-neutral-500">Sirve una sola vez y vence el {expires}. Generar otro invalida este.</p>
    </div>
  );
}
