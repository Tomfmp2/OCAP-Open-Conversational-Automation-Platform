"use client";

import React from "react";
import { X, QrCode, Key, ShieldCheck, AlertCircle, Loader2, MessageSquare, RefreshCw, CheckCircle2 } from "lucide-react";
import { useConnectChannelMutation, useWhatsAppQrConnect } from "../api/useChannelsData";

type ProviderOption = "Telegram" | "WhatsApp" | "WebChat";

interface ChannelConnectModalProps {
  open: boolean;
  onClose: () => void;
}

export function ChannelConnectModal({ open, onClose }: ChannelConnectModalProps) {
  const [selectedProvider, setSelectedProvider] = React.useState<ProviderOption>("Telegram");
  const [displayName, setDisplayName] = React.useState("");
  const [botToken, setBotToken] = React.useState("");
  const [widgetTitle, setWidgetTitle] = React.useState("Asistente OCAP");
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

  const connectMutation = useConnectChannelMutation();
  const {
    connectQr,
    refreshQr,
    qrData,
    connectionState,
    isConnecting,
    isPolling,
    reset: resetQr,
  } = useWhatsAppQrConnect();

  React.useEffect(() => {
    if (!open) {
      resetQr();
      setErrorMessage(null);
    }
  }, [open, resetQr]);

  React.useEffect(() => {
    if (connectionState?.isOpen) {
      const t = setTimeout(() => {
        resetQr();
        setDisplayName("");
        onClose();
      }, 1200);
      return () => clearTimeout(t);
    }
  }, [connectionState?.isOpen, onClose, resetQr]);

  if (!open) return null;

  const handleConnect = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    try {
      if (selectedProvider === "WhatsApp") {
        await connectQr(displayName.trim());
        return;
      }

      await connectMutation.mutateAsync({
        provider: selectedProvider,
        displayName: displayName.trim(),
        botToken: selectedProvider === "Telegram" ? botToken.trim() : undefined,
        widgetTitle: selectedProvider === "WebChat" ? widgetTitle.trim() : undefined,
      });

      setDisplayName("");
      setBotToken("");
      onClose();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Error al conectar con la API de Canales.";
      setErrorMessage(msg);
    }
  };

  const qrSrc = qrData?.qrBase64
    ? qrData.qrBase64.startsWith("data:")
      ? qrData.qrBase64
      : `data:image/png;base64,${qrData.qrBase64.replace(/^data:image\/\w+;base64,/, "")}`
    : null;

  const pending = connectMutation.isPending || isConnecting;
  const linked = Boolean(connectionState?.isOpen);

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-white border border-neutral-200 rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        <div className="p-4 border-b border-neutral-200 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <ShieldCheck className="w-5 h-5 text-neutral-700" />
            <h2 className="text-base font-bold text-neutral-950">Conectar canal</h2>
          </div>
          <button type="button" onClick={onClose} className="text-neutral-500 hover:text-neutral-600">
            <X className="w-4 h-4" />
          </button>
        </div>

        <form onSubmit={(e) => void handleConnect(e)} className="p-6 space-y-5">
          {errorMessage && (
            <div className="p-3 bg-white border-2 border-neutral-950 rounded-md flex items-center gap-2 text-xs text-neutral-950">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{errorMessage}</span>
            </div>
          )}

          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-2">Proveedor disponible</label>
            <div className="grid grid-cols-3 gap-2">
              {(
                [
                  { id: "Telegram" as const, icon: Key },
                  { id: "WhatsApp" as const, icon: QrCode },
                  { id: "WebChat" as const, icon: MessageSquare },
                ] as const
              ).map(({ id, icon: Icon }) => (
                <button
                  key={id}
                  type="button"
                  onClick={() => {
                    setSelectedProvider(id);
                    resetQr();
                    setErrorMessage(null);
                  }}
                  className={`p-3 rounded-xl border text-xs font-semibold flex flex-col items-center gap-1.5 transition-all ${
                    selectedProvider === id
                      ? "border-neutral-950 bg-neutral-100 text-neutral-800"
                      : "border-neutral-200 bg-neutral-50 text-neutral-600 hover:border-neutral-300"
                  }`}
                >
                  <Icon className="w-4 h-4" />
                  <span>{id}</span>
                </button>
              ))}
            </div>
          </div>

          {!qrData && (
            <div className="space-y-2">
              <label className="text-xs font-semibold text-neutral-700 block">Nombre visible</label>
              <input
                type="text"
                required
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder={
                  selectedProvider === "Telegram"
                    ? "Bot de soporte"
                    : selectedProvider === "WhatsApp"
                      ? "WhatsApp Ventas"
                      : "WebChat Portal"
                }
                className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs text-neutral-950 placeholder:text-neutral-500 focus:outline-none focus:ring-1 focus:ring-neutral-950"
              />
            </div>
          )}

          {selectedProvider === "Telegram" && (
            <div className="space-y-2">
              <label className="text-xs font-semibold text-neutral-700 block">Bot API Token</label>
              <input
                type="password"
                required
                placeholder="123456789:ABCdef..."
                value={botToken}
                onChange={(e) => setBotToken(e.target.value)}
                className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs text-neutral-950 font-mono focus:outline-none focus:ring-1 focus:ring-neutral-950"
              />
            </div>
          )}

          {selectedProvider === "WhatsApp" && !qrData && (
            <p className="text-[11px] text-neutral-600 leading-relaxed">
              Se genera un QR con Evolution API (Baileys). Escanealo en WhatsApp → Dispositivos vinculados.
              No necesitas Meta Cloud API ni pagar por número de negocio.
            </p>
          )}

          {selectedProvider === "WhatsApp" && qrData && (
            <div className="space-y-3">
              {linked ? (
                <div className="flex items-center gap-2 text-sm text-emerald-700 font-medium">
                  <CheckCircle2 className="w-5 h-5" />
                  WhatsApp vinculado ({qrData.instanceName})
                </div>
              ) : (
                <>
                  <p className="text-xs text-neutral-700 text-center">
                    Escanea el QR con tu móvil · instancia <span className="font-mono">{qrData.instanceName}</span>
                  </p>
                  {qrSrc ? (
                    <div className="flex justify-center">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={qrSrc}
                        alt="QR WhatsApp Evolution"
                        className="w-56 h-56 border border-neutral-200 rounded-lg bg-white p-2"
                      />
                    </div>
                  ) : (
                    <p className="text-xs text-center text-neutral-500">Esperando QR…</p>
                  )}
                  <div className="flex items-center justify-center gap-2 text-[11px] text-neutral-500">
                    {isPolling && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
                    Estado: {connectionState?.state ?? "conectando…"}
                  </div>
                  <button
                    type="button"
                    onClick={() => void refreshQr().catch((err: unknown) => {
                      setErrorMessage(err instanceof Error ? err.message : "No se pudo refrescar el QR");
                    })}
                    className="w-full flex items-center justify-center gap-1.5 text-xs text-neutral-700 hover:bg-neutral-100 rounded-lg py-2"
                  >
                    <RefreshCw className="w-3.5 h-3.5" />
                    Refrescar QR
                  </button>
                </>
              )}
            </div>
          )}

          {selectedProvider === "WebChat" && (
            <div className="space-y-2">
              <label className="text-xs font-semibold text-neutral-700 block">Título del widget</label>
              <input
                type="text"
                value={widgetTitle}
                onChange={(e) => setWidgetTitle(e.target.value)}
                className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs focus:outline-none focus:ring-1 focus:ring-neutral-950"
              />
              <p className="text-[10px] text-neutral-500">
                Tras conectar, prueba el widget en /channels/webchat.
              </p>
            </div>
          )}

          {!qrData && (
            <div className="flex justify-end gap-2 pt-2 border-t border-neutral-200">
              <button
                type="button"
                onClick={onClose}
                disabled={pending}
                className="px-4 py-2 rounded-lg text-xs font-medium text-neutral-600 hover:bg-neutral-100 disabled:opacity-50"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={pending}
                className="px-4 py-2 rounded-lg bg-neutral-950 hover:bg-neutral-900 text-white text-xs font-semibold flex items-center gap-1.5 disabled:opacity-50"
              >
                {pending && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
                <span>
                  {pending
                    ? "Conectando..."
                    : selectedProvider === "WhatsApp"
                      ? "Generar QR"
                      : "Guardar y conectar"}
                </span>
              </button>
            </div>
          )}

          {qrData && !linked && (
            <div className="flex justify-end pt-2 border-t border-neutral-200">
              <button
                type="button"
                onClick={() => {
                  resetQr();
                  onClose();
                }}
                className="px-4 py-2 rounded-lg text-xs font-medium text-neutral-600 hover:bg-neutral-100"
              >
                Cerrar
              </button>
            </div>
          )}
        </form>
      </div>
    </div>
  );
}
