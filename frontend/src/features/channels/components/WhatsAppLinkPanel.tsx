"use client";

import { CheckCircle2, Loader2 } from "lucide-react";
import { Button, FormError } from "@/shared/components/ui";
import { useWhatsAppLink } from "../api/useWhatsAppLink";

interface WhatsAppLinkPanelProps {
  active: boolean;
  /** En el alta, "Listo" cierra el diálogo; en la configuración del canal no hace falta. */
  onLinked?: () => void;
}

function toImageSrc(qr: string) {
  return qr.startsWith("data:") ? qr : `data:image/png;base64,${qr}`;
}

const STEPS = ["Abre WhatsApp en el teléfono del negocio.", "Ve a Dispositivos vinculados y toca Vincular un dispositivo.", "Escanea el código."];

/** Vincula el WhatsApp de la organización escaneando un QR desde el teléfono, como WhatsApp Web. */
export function WhatsAppLinkPanel({ active, onLinked }: WhatsAppLinkPanelProps) {
  const { status, error, busy, link, unlink } = useWhatsAppLink(active);

  return (
    <div className="space-y-4" aria-live="polite">
      <FormError error={error ? new Error(error) : null} />

      {!status && !error && (
        <p className="flex items-center gap-2 text-sm text-neutral-600">
          <Loader2 className="h-4 w-4 animate-spin motion-reduce:animate-none" aria-hidden /> Consultando WhatsApp…
        </p>
      )}

      {status?.state === "linked" && (
        <div className="space-y-4">
          <p className="flex items-center gap-2 text-sm font-medium text-success">
            <CheckCircle2 className="h-4 w-4" aria-hidden />
            Vinculado{status.phoneNumber ? ` al +${status.phoneNumber}` : ""}
          </p>
          <p className="text-sm text-neutral-600">El agente responde los chats individuales de este número. Los grupos se ignoran.</p>
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => void unlink()} loading={busy}>
              Desvincular
            </Button>
            {onLinked && <Button onClick={onLinked}>Listo</Button>}
          </div>
        </div>
      )}

      {status?.state === "unlinked" && (
        <div className="space-y-4">
          <p className="text-sm text-neutral-600">Usa el número normal del negocio: no hace falta WhatsApp Business ni una cuenta de Meta.</p>
          <div className="flex justify-end">
            <Button onClick={() => void link()} loading={busy}>
              Generar QR
            </Button>
          </div>
        </div>
      )}

      {status?.state === "pending" && (
        <div className="grid gap-5 sm:grid-cols-[auto_minmax(0,1fr)] sm:items-center">
          {status.qrBase64 ? (
            // eslint-disable-next-line @next/next/no-img-element -- data URL generada por Evolution
            <img
              src={toImageSrc(status.qrBase64)}
              alt="Código QR para vincular WhatsApp"
              className="mx-auto h-52 w-52 rounded-lg border border-neutral-200 bg-white p-2"
            />
          ) : (
            <Button variant="secondary" onClick={() => void link()} loading={busy}>
              Mostrar QR
            </Button>
          )}
          <div className="space-y-3">
            <ol className="list-decimal space-y-1 pl-5 text-sm text-neutral-700">
              {STEPS.map((step) => (
                <li key={step}>{step}</li>
              ))}
            </ol>
            {status.pairingCode && (
              <p className="text-sm text-neutral-700">
                O ingresa el código <span className="font-mono font-semibold text-neutral-950">{status.pairingCode}</span>
              </p>
            )}
            <p className="flex items-center gap-2 text-sm text-neutral-600">
              <Loader2 className="h-4 w-4 animate-spin motion-reduce:animate-none" aria-hidden /> Esperando el escaneo
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
