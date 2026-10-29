"use client";

import React from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/shared/api/apiClient";
import { googleRedirectUri } from "@/features/settings/api/useOrganization";
import { Page, PageHeader } from "@/shared/components/ui";

/** Google vuelve aquí con el código y el state; la API los valida y guarda la conexión cifrada. */
export default function GoogleCallbackPage() {
  return (
    <React.Suspense fallback={null}>
      <GoogleCallback />
    </React.Suspense>
  );
}

function GoogleCallback() {
  const params = useSearchParams();
  const queryClient = useQueryClient();
  const code = params.get("code");
  const denied = !!params.get("error") || !code;
  const [exchange, setExchange] = React.useState<{ ok: boolean; message: string } | null>(null);
  const result = denied ? { ok: false, message: "Google no autorizó la conexión. Puedes intentarlo de nuevo desde Ajustes." } : exchange;
  const started = React.useRef(false);

  React.useEffect(() => {
    // Una sola vez: el código de Google sirve para un único intercambio.
    if (started.current || denied) return;
    started.current = true;
    apiClient
      .post("/api/integrations/google/connect", { authCode: code, state: params.get("state"), redirectUri: googleRedirectUri() }, { skipRetry: true })
      .then(() => {
        void queryClient.invalidateQueries({ queryKey: ["organization", "google"] });
        setExchange({ ok: true, message: "Cuenta de Google conectada. El asistente ya puede usar Gmail, Calendar y Sheets." });
      })
      .catch((err: unknown) => setExchange({ ok: false, message: err instanceof Error ? err.message : "No se pudo conectar la cuenta de Google." }));
  }, [code, denied, params, queryClient]);

  return (
    <Page>
      <PageHeader title="Conectar Google" />
      <p role="status" className={result ? (result.ok ? "text-sm text-success" : "text-sm text-danger") : "text-sm text-neutral-600"}>
        {result?.message ?? "Conectando tu cuenta de Google…"}
      </p>
      {result && (
        <Link href="/settings" className="mt-4 inline-block text-sm font-medium text-neutral-950 underline underline-offset-2">
          Volver a Ajustes
        </Link>
      )}
    </Page>
  );
}
