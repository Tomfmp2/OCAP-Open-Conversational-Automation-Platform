"use client";

import React from "react";
import { X, QrCode, Key, ShieldCheck, AlertCircle, Loader2 } from "lucide-react";
import { useConnectChannelMutation } from "../api/useChannelsData";

interface ChannelConnectModalProps {
  open: boolean;
  onClose: () => void;
}

export function ChannelConnectModal({ open, onClose }: ChannelConnectModalProps) {
  const [selectedProvider, setSelectedProvider] = React.useState<"Telegram" | "WhatsApp" | "Google">("Telegram");
  const [botToken, setBotToken] = React.useState("");
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

  const connectMutation = useConnectChannelMutation();

  if (!open) return null;

  const handleConnect = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    try {
      await connectMutation.mutateAsync({
        provider: selectedProvider,
        botToken: selectedProvider === "Telegram" ? botToken.trim() : undefined,
        authCode: selectedProvider === "Google" ? "google_oauth_code_sample" : undefined
      });

      setBotToken("");
      onClose();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Error al conectar con la API de Canales.";
      setErrorMessage(msg);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        <div className="p-4 border-b border-zinc-200 dark:border-zinc-800 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <ShieldCheck className="w-5 h-5 text-blue-500" />
            <h2 className="text-base font-bold text-zinc-900 dark:text-zinc-100">Conectar Nuevo Adaptador de Canal</h2>
          </div>
          <button onClick={onClose} className="text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200">
            <X className="w-4 h-4" />
          </button>
        </div>

        <form onSubmit={handleConnect} className="p-6 space-y-5">
          {errorMessage && (
            <div className="p-3 bg-red-500/10 border border-red-500/20 rounded-lg flex items-center gap-2 text-xs text-red-500">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{errorMessage}</span>
            </div>
          )}

          <>
              <div>
                <label className="text-xs font-semibold text-zinc-700 dark:text-zinc-300 block mb-2">
                  Seleccionar Proveedor
                </label>
                <div className="grid grid-cols-3 gap-2">
                  {(["Telegram", "WhatsApp", "Google"] as const).map((prov) => (
                    <button
                      key={prov}
                      type="button"
                      onClick={() => setSelectedProvider(prov)}
                      className={`p-3 rounded-xl border text-xs font-semibold flex flex-col items-center gap-1.5 transition-all ${
                        selectedProvider === prov
                          ? "border-blue-500 bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400"
                          : "border-zinc-200 dark:border-zinc-800 bg-zinc-50 dark:bg-zinc-950/40 text-zinc-600 dark:text-zinc-400 hover:border-zinc-300"
                      }`}
                    >
                      {prov === "WhatsApp" ? <QrCode className="w-4 h-4" /> : <Key className="w-4 h-4" />}
                      <span>{prov}</span>
                    </button>
                  ))}
                </div>
              </div>

              {selectedProvider === "Telegram" && (
                <div className="space-y-2">
                  <label className="text-xs font-semibold text-zinc-700 dark:text-zinc-300 block">
                    Bot API Token (de @BotFather)
                  </label>
                  <input
                    type="password"
                    required
                    placeholder="123456789:ABCdefGHIjklMNOpqrsTUVwxyZ"
                    value={botToken}
                    onChange={(e) => setBotToken(e.target.value)}
                    className="w-full bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-2 text-xs text-zinc-900 dark:text-zinc-100 placeholder:text-zinc-400 focus:outline-none focus:ring-1 focus:ring-blue-500 font-mono"
                  />
                  <p className="text-[10px] text-zinc-400">
                    Las credenciales serán cifradas inmediatamente en el Credential Vault con AES-256.
                  </p>
                </div>
              )}

              {selectedProvider === "WhatsApp" && (
                <div className="text-center py-4 space-y-3 bg-zinc-50 dark:bg-zinc-950/50 rounded-xl border border-zinc-200 dark:border-zinc-800">
                  <div className="w-32 h-32 bg-white border border-zinc-300 dark:border-zinc-700 mx-auto rounded-lg flex items-center justify-center text-zinc-400">
                    <QrCode className="w-24 h-24 text-zinc-800 dark:text-zinc-200" />
                  </div>
                  <p className="text-xs text-zinc-500">Escanea este código QR desde tu aplicación de WhatsApp Business</p>
                </div>
              )}

              {selectedProvider === "Google" && (
                <div className="p-4 bg-zinc-50 dark:bg-zinc-950/50 rounded-xl border border-zinc-200 dark:border-zinc-800 space-y-2 text-center">
                  <p className="text-xs text-zinc-600 dark:text-zinc-400">
                    Inicia sesión con tu cuenta institucional de Google Workspace para conceder permisos de lectura a Gmail y Calendar.
                  </p>
                </div>
              )}

              <div className="flex justify-end gap-2 pt-2 border-t border-zinc-200 dark:border-zinc-800">
                <button
                  type="button"
                  onClick={onClose}
                  disabled={connectMutation.isPending}
                  className="px-4 py-2 rounded-lg text-xs font-medium text-zinc-600 dark:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-zinc-800 disabled:opacity-50"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={connectMutation.isPending}
                  className="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-colors flex items-center gap-1.5 disabled:opacity-50"
                >
                  {connectMutation.isPending && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
                  <span>{connectMutation.isPending ? "Conectando..." : "Guardar & Conectar"}</span>
                </button>
              </div>
            </>
        </form>
      </div>
    </div>
  );
}
