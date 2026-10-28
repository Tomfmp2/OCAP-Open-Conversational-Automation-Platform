import React from "react";

/** Bloque de una pantalla de ajustes: qué es a la izquierda, el control a la derecha (apilado en móvil). */
export function Section({ title, description, children }: { title: string; description?: string; children: React.ReactNode }) {
  return (
    <section className="grid gap-4 border-t border-neutral-200 pt-6 md:grid-cols-[minmax(0,1fr)_minmax(0,2fr)] md:gap-8">
      <div>
        <h2 className="text-sm font-semibold text-neutral-950">{title}</h2>
        {description && <p className="mt-1 text-sm text-neutral-600">{description}</p>}
      </div>
      <div className="min-w-0">{children}</div>
    </section>
  );
}
