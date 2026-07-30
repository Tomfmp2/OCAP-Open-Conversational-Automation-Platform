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
    <main className="flex min-h-screen items-center justify-center bg-zinc-50 px-6 dark:bg-zinc-950">
      <section className="max-w-md text-center">
        <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-100">
          Algo salió mal
        </h1>
        <p className="mt-3 text-sm text-zinc-600 dark:text-zinc-400">
          No pudimos completar esta operación. Puedes volver a intentarlo.
        </p>
        <button
          type="button"
          onClick={reset}
          className="mt-6 rounded-md bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-700 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-white"
        >
          Reintentar
        </button>
      </section>
    </main>
  );
}
