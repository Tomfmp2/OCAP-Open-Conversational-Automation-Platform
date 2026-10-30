"use client";

import React from "react";
import { Badge, Button, FormError, Input, Section } from "@/shared/components/ui";
import { QrCode } from "@/shared/components/ui/QrCode";
import { useAccountActions, useMfaStatus } from "../api/useAccount";
import { RecoveryCodes } from "./RecoveryCodes";

export function MfaCard() {
  const status = useMfaStatus();
  const { startMfa, enableMfa, disableMfa, regenerateCodes } = useAccountActions();
  const [code, setCode] = React.useState("");
  const [codes, setCodes] = React.useState<string[] | null>(null);
  const [disabling, setDisabling] = React.useState(false);
  const setup = startMfa.data;

  return (
    <Section title="Verificación en dos pasos" description="Además de la contraseña, pide un código de tu teléfono al entrar.">
      <div className="space-y-4">
        {codes && <RecoveryCodes codes={codes} />}
        {status.isLoading && <div className="h-9 w-40 animate-pulse rounded-md bg-neutral-100 motion-reduce:animate-none" role="status" aria-label="Cargando" />}
        <FormError error={status.error} />

        {status.data?.enabled ? (
          <>
            <p className="flex flex-wrap items-center gap-2 text-sm text-neutral-700">
              <Badge tone="success">Activa</Badge>
              Te quedan {status.data.recoveryCodesLeft} códigos de recuperación.
            </p>
            {disabling ? (
              <form
                className="max-w-sm space-y-4"
                onSubmit={(e) => {
                  e.preventDefault();
                  disableMfa.mutate(code.trim(), {
                    onSuccess: () => {
                      setDisabling(false);
                      setCode("");
                      setCodes(null);
                    },
                  });
                }}
              >
                <Input label="Código de tu app" inputMode="numeric" autoComplete="one-time-code" required value={code} onChange={(e) => setCode(e.target.value)} />
                <FormError error={disableMfa.error} />
                <div className="flex gap-2">
                  <Button type="submit" variant="danger" loading={disableMfa.isPending}>
                    Desactivar
                  </Button>
                  <Button variant="ghost" onClick={() => setDisabling(false)}>
                    Cancelar
                  </Button>
                </div>
              </form>
            ) : (
              <div className="flex flex-wrap gap-2">
                <Button variant="secondary" loading={regenerateCodes.isPending} onClick={() => regenerateCodes.mutate(undefined, { onSuccess: setCodes })}>
                  Generar códigos nuevos
                </Button>
                <Button variant="ghost" onClick={() => setDisabling(true)}>
                  Desactivar
                </Button>
              </div>
            )}
            <FormError error={regenerateCodes.error} />
          </>
        ) : status.data && setup ? (
          <form
            className="space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              enableMfa.mutate(code.trim(), {
                onSuccess: (result) => {
                  setCodes(result.recoveryCodes);
                  setCode("");
                  startMfa.reset();
                },
              });
            }}
          >
            <p className="text-sm text-neutral-700">Escanea el código con tu app autenticadora (Google Authenticator, 1Password u otra).</p>
            <div className="w-fit rounded-lg border border-neutral-200 bg-white p-2">
              <QrCode value={setup.qrCodeUri} label="Código QR para tu app autenticadora" />
            </div>
            <details className="text-sm text-neutral-700">
              <summary className="focus-ring cursor-pointer rounded text-neutral-950">No puedo escanearlo</summary>
              <p className="mt-2">Escribe esta clave en la app:</p>
              <p className="mt-1 break-all rounded-md bg-neutral-100 px-2 py-1 font-mono text-sm text-neutral-900">{setup.secret}</p>
            </details>
            <div className="max-w-xs">
              <Input label="Código de 6 dígitos" inputMode="numeric" autoComplete="one-time-code" required value={code} onChange={(e) => setCode(e.target.value)} />
            </div>
            <FormError error={enableMfa.error} />
            <Button type="submit" loading={enableMfa.isPending}>
              Activar
            </Button>
          </form>
        ) : status.data ? (
          <>
            <Button loading={startMfa.isPending} onClick={() => startMfa.mutate()}>
              Configurar
            </Button>
            <FormError error={startMfa.error} />
          </>
        ) : null}
      </div>
    </Section>
  );
}
