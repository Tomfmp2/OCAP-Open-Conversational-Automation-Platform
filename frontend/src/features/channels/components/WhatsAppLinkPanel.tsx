"use client";

import { AlertCircle, CheckCircle2, Loader2, QrCode, Unlink } from "lucide-react";
import { Button } from "@/shared/components/ui/Button";
import { useWhatsAppLink } from "../api/useWhatsAppLink";

interface WhatsAppLinkPanelProps {
  active: boolean;
  onLinked: () => void;
}

function toImageSrc(qr: string) {
  return qr.startsWith("data:") ? qr : `data:image/png;base64,${qr}`;
}

/** Vincula el WhatsApp de la organización escaneando un QR desde el teléfono, como WhatsApp Web. */
export function WhatsAppLinkPanel({ active, onLinked }: WhatsAppLinkPanelProps) {
  const { status, error, busy, live, link, unlink } = useWhatsAppLink(active);

  return (
    <div className="space-y-4" aria-live="polite">
      {error && (
        <div role="alert" className="flex items-center gap-2 rounded-md border-2 border-neutral-950 p-3 text-xs text-neutral-950">
          <AlertCircle className="h-4 w-4 shrink-0" aria-hidden />
          <span>{error}</span>
        </div>
      )}

      {!status && !error && (
        <p className="flex items-center gap-2 text-xs text-neutral-600">
          <Loader2 className="h-3.5 w-3.5 animate-spin motion-reduce:animate-none" aria-hidden /> Consultando estado…
        </p>
      )}

      {status?.state === "linked" && (
        <div className="space-y-3">
          <p className="flex items-center gap-2 text-sm font-medium text-emerald-700">
            <CheckCircle2 className="h-5 w-5" aria-hidden />
            WhatsApp vinculado{status.phoneNumber ? ` · +${status.phoneNumber}` : ""}
          </p>
          <p className="text-[11px] leading-relaxed text-neutral-600">
            El asistente ya responde los chats individuales de este número. Los grupos y estados se ignoran.
          </p>
          <div className="flex justify-end gap-2 border-t border-neutral-200 pt-3">
            <Button type="button" variant="secondary" size="sm" onClick={() => void unlink()} loading={busy}>
              <Unlink className="h-3.5 w-3.5" aria-hidden /> Desvincular
            </Button>
            <Button type="button" size="sm" onClick={onLinked}>
              Listo
            </Button>
          </div>
        </div>
      )}

      {status?.state === "unlinked" && (
        <div className="space-y-3">
          <p className="text-[11px] leading-relaxed text-neutral-600">
            Se mostrará un QR. En tu teléfono abre WhatsApp → Dispositivos vinculados → Vincular un dispositivo y
            escanéalo. No necesitas cuenta de WhatsApp Business ni Meta Cloud API.
          </p>
          <div className="flex justify-end border-t border-neutral-200 pt-3">
            <Button type="button" size="sm" onClick={() => void link()} loading={busy}>
              <QrCode className="h-3.5 w-3.5" aria-hidden /> Generar QR
            </Button>
          </div>
        </div>
      )}

      {status?.state === "pending" && (
        <div className="space-y-3">
          <p className="text-center text-xs text-neutral-700">
            WhatsApp → Dispositivos vinculados → Vincular un dispositivo
          </p>
          {status.qrBase64 ? (
            <div className="flex justify-center">
              {/* eslint-disable-next-line @next/next/no-img-element -- data URL generada por Evolution */}
              <img
                src={toImageSrc(status.qrBase64)}
                alt="Código QR para vincular WhatsApp"
                className="h-56 w-56 rounded-lg border border-neutral-200 bg-white p-2"
              />
            </div>
          ) : (
            <div className="flex justify-center">
              <Button type="button" variant="secondary" size="sm" onClick={() => void link()} loading={busy}>
                <QrCode className="h-3.5 w-3.5" aria-hidden /> Mostrar QR
              </Button>
            </div>
          )}
          {status.pairingCode && (
            <p className="text-center text-xs text-neutral-700">
              Código de vinculación: <span className="font-mono font-semibold">{status.pairingCode}</span>
            </p>
          )}
          <p className="flex items-center justify-center gap-2 text-[11px] text-neutral-500">
            <Loader2 className="h-3.5 w-3.5 animate-spin motion-reduce:animate-none" aria-hidden />
            Esperando el escaneo{live ? "" : " (actualizando cada pocos segundos)"}
          </p>
        </div>
      )}
    </div>
  );
}
