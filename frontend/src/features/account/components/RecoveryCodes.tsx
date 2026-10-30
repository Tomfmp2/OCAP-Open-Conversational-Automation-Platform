"use client";

import { CopyButton } from "@/shared/components/ui";

/** Se muestran una sola vez: el servidor solo guarda su hash. */
export function RecoveryCodes({ codes }: { codes: string[] }) {
  return (
    <div className="space-y-3 rounded-lg border border-warning/30 bg-warning-subtle p-4">
      <p className="text-sm font-medium text-neutral-950">Guarda estos códigos de recuperación</p>
      <p className="text-sm text-neutral-700">Si pierdes el teléfono, cada código te deja entrar una vez. No se vuelven a mostrar.</p>
      <ul className="grid grid-cols-2 gap-1.5 font-mono text-sm text-neutral-900">
        {codes.map((code) => (
          <li key={code}>{code}</li>
        ))}
      </ul>
      <CopyButton value={codes.join("\n")} label="Copiar códigos" />
    </div>
  );
}
