"use client";

import React from "react";
import { Check, Copy } from "lucide-react";
import { Button } from "./Button";

/** Copia un texto al portapapeles y lo confirma en el botón y para el lector de pantalla. */
export function CopyButton({ value, label = "Copiar" }: { value: string; label?: string }) {
  const [copied, setCopied] = React.useState(false);

  React.useEffect(() => {
    if (!copied) return;
    const timer = setTimeout(() => setCopied(false), 2000);
    return () => clearTimeout(timer);
  }, [copied]);

  return (
    <Button
      variant="secondary"
      size="sm"
      onClick={() =>
        // Sin portapapeles (http sin TLS) no hay nada que confirmar: el texto queda seleccionable al lado.
        navigator.clipboard?.writeText(value).then(() => setCopied(true), () => setCopied(false))
      }
    >
      {copied ? <Check className="h-3.5 w-3.5" aria-hidden /> : <Copy className="h-3.5 w-3.5" aria-hidden />}
      <span aria-live="polite">{copied ? "Copiado" : label}</span>
    </Button>
  );
}
