"use client";

import React from "react";
import { ShieldCheck } from "lucide-react";
import { Badge } from "@/shared/components/ui/Badge";
import { Button } from "@/shared/components/ui/Button";
import { Input } from "@/shared/components/ui/Input";
import { QrCode } from "@/shared/components/ui/QrCode";
import { Surface } from "@/shared/components/ui/Surface";
import { useAccountActions, useMfaStatus } from "../api/useAccount";
import { RecoveryCodes } from "./RecoveryCodes";

function ErrorText({ error }: { error: unknown }) {
  if (!error) return null;
  return (
    <p role="alert" className="text-xs text-neutral-950">
      {error instanceof Error ? error.message : "No se pudo completar la acción."}
    </p>
  );
}

export function MfaCard() {
  const status = useMfaStatus();
  const { startMfa, enableMfa, disableMfa, regenerateCodes } = useAccountActions();
  const [code, setCode] = React.useState("");
  const [codes, setCodes] = React.useState<string[] | null>(null);
  const [disabling, setDisabling] = React.useState(false);
  const setup = startMfa.data;

  return (
    <Surface className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <ShieldCheck className="h-4 w-4 text-neutral-500" aria-hidden />
          <h2 className="text-sm font-semibold text-neutral-950">Verificación en dos pasos</h2>
        </div>
        {status.data && <Badge tone={status.data.enabled ? "success" : "neutral"}>{status.data.enabled ? "Activa" : "Inactiva"}</Badge>}
      </div>

      {codes && <RecoveryCodes codes={codes} />}

      {status.isLoading && <div className="h-10 animate-pulse rounded-md bg-neutral-100 motion-reduce:animate-none" aria-busy="true" />}
      {status.isError && <ErrorText error={status.error} />}

      {status.data?.enabled ? (
        <div className="space-y-3">
          <p className="text-sm text-neutral-700">
            Al iniciar sesión te pedimos el código de tu app autenticadora. Te quedan {status.data.recoveryCodesLeft} códigos de
            recuperación.
          </p>
          {disabling ? (
            <form
              className="flex flex-wrap items-end gap-2"
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
              <div className="w-48">
                <Input id="mfa-disable-code" label="Código actual" inputMode="numeric" autoComplete="one-time-code" required value={code} onChange={(e) => setCode(e.target.value)} />
              </div>
              <Button type="submit" size="sm" loading={disableMfa.isPending}>
                Desactivar
              </Button>
              <Button type="button" variant="ghost" size="sm" onClick={() => setDisabling(false)}>
                Cancelar
              </Button>
            </form>
          ) : (
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="secondary" size="sm" loading={regenerateCodes.isPending} onClick={() => regenerateCodes.mutate(undefined, { onSuccess: setCodes })}>
                Generar códigos nuevos
              </Button>
              <Button type="button" variant="ghost" size="sm" onClick={() => setDisabling(true)}>
                Desactivar
              </Button>
            </div>
          )}
          <ErrorText error={disableMfa.error ?? regenerateCodes.error} />
        </div>
      ) : status.data && setup ? (
        <div className="grid gap-4 sm:grid-cols-[auto,1fr] sm:items-start">
          <div className="rounded-md border border-neutral-200 bg-white p-2">
            <QrCode value={setup.qrCodeUri} label="Código QR para tu app autenticadora" />
          </div>
          <form
            className="space-y-3"
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
            <p className="text-sm text-neutral-700">
              Escanéalo con Google Authenticator, Microsoft Authenticator, 1Password u otra app compatible. Si no puedes
              escanear, ingresa esta clave:
            </p>
            <p className="break-all rounded bg-neutral-100 px-2 py-1 font-mono text-xs text-neutral-900">{setup.secret}</p>
            <div className="w-48">
              <Input id="mfa-enable-code" label="Código de 6 dígitos" inputMode="numeric" autoComplete="one-time-code" required value={code} onChange={(e) => setCode(e.target.value)} />
            </div>
            <Button type="submit" size="sm" loading={enableMfa.isPending}>
              Activar
            </Button>
            <ErrorText error={enableMfa.error} />
          </form>
        </div>
      ) : status.data ? (
        <div className="space-y-3">
          <p className="text-sm text-neutral-700">
            Además de tu contraseña, pide un código de tu teléfono al iniciar sesión. Protege la cuenta aunque se filtre la
            contraseña.
          </p>
          <Button type="button" size="sm" loading={startMfa.isPending} onClick={() => startMfa.mutate()}>
            Configurar
          </Button>
          <ErrorText error={startMfa.error} />
        </div>
      ) : null}
    </Surface>
  );
}
