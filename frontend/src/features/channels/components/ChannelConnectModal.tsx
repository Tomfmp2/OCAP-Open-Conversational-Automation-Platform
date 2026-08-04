"use client";

import React from "react";
import { X, QrCode, Key, ShieldCheck, AlertCircle, Loader2, MessageSquare } from "lucide-react";
import { useConnectChannelMutation } from "../api/useChannelsData";

type ProviderOption = "Telegram" | "WhatsApp" | "WebChat";

interface ChannelConnectModalProps {
  open: boolean;
  onClose: () => void;
}

export function ChannelConnectModal({ open, onClose }: ChannelConnectModalProps) {
  const [selectedProvider, setSelectedProvider] = React.useState<ProviderOption>("Telegram");
  const [displayName, setDisplayName] = React.useState("");
  const [botToken, setBotToken] = React.useState("");
  const [phoneNumberId, setPhoneNumberId] = React.useState("");
  const [apiToken, setApiToken] = React.useState("");
  const [widgetTitle, setWidgetTitle] = React.useState("Asistente OCAP");
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

  const connectMutation = useConnectChannelMutation();

  if (!open) return null;

  const handleConnect = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    try {
      await connectMutation.mutateAsync({
        provider: selectedProvider,
        displayName: displayName.trim(),
        botToken: selectedProvider === "Telegram" ? botToken.trim() : undefined,
        phoneNumberId: selectedProvider === "WhatsApp" ? phoneNumberId.trim() : undefined,
        apiToken: selectedProvider === "WhatsApp" ? apiToken.trim() : undefined,
        widgetTitle: selectedProvider === "WebChat" ? widgetTitle.trim() : undefined,
      });

      setDisplayName("");
      setBotToken("");
      setPhoneNumberId("");
      setApiToken("");
      onClose();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Error al conectar con la API de Canales.";
      setErrorMessage(msg);
    }
  };

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
                  onClick={() => setSelectedProvider(id)}
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
            <p className="mt-2 text-[10px] text-neutral-500">
              Slack, Discord, Teams y Google Workspace como canal están marcados como próximamente en el catálogo API.
            </p>
          </div>

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

          {selectedProvider === "WhatsApp" && (
            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-xs font-semibold text-neutral-700 block">Phone Number ID</label>
                <input
                  type="text"
                  required
                  value={phoneNumberId}
                  onChange={(e) => setPhoneNumberId(e.target.value)}
                  className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs font-mono focus:outline-none focus:ring-1 focus:ring-neutral-950"
                />
              </div>
              <div className="space-y-2">
                <label className="text-xs font-semibold text-neutral-700 block">API Token</label>
                <input
                  type="password"
                  required
                  value={apiToken}
                  onChange={(e) => setApiToken(e.target.value)}
                  className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs font-mono focus:outline-none focus:ring-1 focus:ring-neutral-950"
                />
              </div>
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

          <div className="flex justify-end gap-2 pt-2 border-t border-neutral-200">
            <button
              type="button"
              onClick={onClose}
              disabled={connectMutation.isPending}
              className="px-4 py-2 rounded-lg text-xs font-medium text-neutral-600 hover:bg-neutral-100 disabled:opacity-50"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={connectMutation.isPending}
              className="px-4 py-2 rounded-lg bg-neutral-950 hover:bg-neutral-900 text-white text-xs font-semibold flex items-center gap-1.5 disabled:opacity-50"
            >
              {connectMutation.isPending && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
              <span>{connectMutation.isPending ? "Conectando..." : "Guardar y conectar"}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
