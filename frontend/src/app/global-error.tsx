"use client";

import { useEffect } from "react";

export default function GlobalError({
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
    <html lang="es">
      <body>
        <main
          style={{
            minHeight: "100vh",
            display: "grid",
            placeItems: "center",
            padding: "24px",
            fontFamily: "sans-serif",
            textAlign: "center",
          }}
        >
          <section>
            <h1>OCAP no pudo cargar</h1>
            <p>Se produjo un error inesperado. Intenta cargar la aplicación de nuevo.</p>
            <button type="button" onClick={reset}>
              Reintentar
            </button>
          </section>
        </main>
      </body>
    </html>
  );
}
