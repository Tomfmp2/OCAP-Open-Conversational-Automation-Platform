"use client";

import React from "react";
import { Copy } from "lucide-react";
import { Button } from "@/shared/components/ui/Button";

/** Se muestran una sola vez: el servidor solo guarda su hash. */
export function RecoveryCodes({ codes }: { codes: string[] }) {
  const [copied, setCopied] = React.useState(false);
  return (
    <div className="space-y-3 rounded-md border-2 border-neutral-950 bg-white p-4">
      <p className="text-sm font-semibold text-neutral-950">Guarda tus códigos de recuperación</p>
      <p className="text-xs text-neutral-600">
        Cada uno sirve una vez si pierdes el teléfono. No los volveremos a mostrar.
      </p>
      <ul className="grid grid-cols-2 gap-1.5 font-mono text-sm text-neutral-900">
        {codes.map((code) => (
          <li key={code}>{code}</li>
        ))}
      </ul>
      <Button
        type="button"
        variant="secondary"
        size="sm"
        onClick={() => {
          void navigator.clipboard.writeText(codes.join("\n")).then(() => setCopied(true));
        }}
      >
        <Copy className="h-3.5 w-3.5" aria-hidden /> {copied ? "Copiados" : "Copiar códigos"}
      </Button>
    </div>
  );
}
