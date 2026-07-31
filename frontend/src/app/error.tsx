"use client";

import { useEffect } from "react";

export default function ErrorBoundary({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <main className="flex min-h-screen items-center justify-center bg-neutral-50 px-6">
      <section className="max-w-md text-center">
        <h1 className="text-2xl font-semibold text-neutral-950">Algo salió mal</h1>
        <p className="mt-3 text-sm text-neutral-600">
          No pudimos completar esta operación. Puedes volver a intentarlo.
        </p>
        <button
          type="button"
          onClick={reset}
          className="mt-6 rounded-md bg-neutral-950 px-4 py-2 text-sm font-medium text-white hover:bg-neutral-800"
        >
          Reintentar
        </button>
      </section>
    </main>
  );
}
