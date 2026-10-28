"use client";

import React from "react";

const INTERVAL_MS = 30_000;

/** Comprueba /health/live cada 30 s y al recuperar la red; true mientras no se demuestre lo contrario. */
export function useApiReachable(): boolean {
  const [reachable, setReachable] = React.useState(true);

  React.useEffect(() => {
    let cancelled = false;
    const check = () =>
      fetch(`${process.env.NEXT_PUBLIC_API_URL || ""}/api/health`, { cache: "no-store" })
        .then((r) => !cancelled && setReachable(r.ok))
        .catch(() => !cancelled && setReachable(false));

    const timer = setInterval(check, INTERVAL_MS);
    window.addEventListener("online", check);
    return () => {
      cancelled = true;
      clearInterval(timer);
      window.removeEventListener("online", check);
    };
  }, []);

  return reachable;
}
