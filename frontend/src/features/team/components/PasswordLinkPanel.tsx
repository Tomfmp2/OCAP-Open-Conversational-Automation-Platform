"use client";

import { CopyButton } from "@/shared/components/ui";
import { absoluteLink, type PasswordLink } from "../api/useTeam";

/** El enlace de un solo uso para que la persona defina su contraseña. */
export function PasswordLinkPanel({ link, recipient }: { link: PasswordLink; recipient: string }) {
  const url = absoluteLink(link);
  const expires = new Date(link.expiresAtUtc).toLocaleString();

  return (
    <div className="space-y-3">
      <p className="text-sm text-neutral-700">
        {link.emailed
          ? `Enviamos el enlace a ${recipient}. Si no le llega, compártelo por otro medio.`
          : `Envía este enlace a ${recipient} por un medio privado para que defina su contraseña.`}
      </p>
      <div className="flex items-center gap-2">
        <input
          readOnly
          value={url}
          aria-label="Enlace para definir la contraseña"
          onFocus={(e) => e.currentTarget.select()}
          className="focus-ring h-9 min-w-0 flex-1 rounded-md border border-neutral-300 bg-neutral-50 px-3 text-sm text-neutral-800"
        />
        <CopyButton value={url} />
      </div>
      <p className="text-xs text-neutral-600">Funciona una sola vez y vence el {expires}.</p>
    </div>
  );
}
