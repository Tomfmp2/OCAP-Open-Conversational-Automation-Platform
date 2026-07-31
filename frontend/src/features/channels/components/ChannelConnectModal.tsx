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
 const [displayName, setDisplayName] = React.useState("");
 const [botToken, setBotToken] = React.useState("");
 const [phoneNumberId, setPhoneNumberId] = React.useState("");
 const [apiToken, setApiToken] = React.useState("");
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
 phoneNumberId:
 selectedProvider === "WhatsApp" ? phoneNumberId.trim() : undefined,
 apiToken: selectedProvider === "WhatsApp" ? apiToken.trim() : undefined,
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
 <h2 className="text-base font-bold text-neutral-950">Conectar Nuevo Adaptador de Canal</h2>
 </div>
 <button onClick={onClose} className="text-neutral-500 hover:text-neutral-600">
 <X className="w-4 h-4" />
 </button>
 </div>

 <form onSubmit={handleConnect} className="p-6 space-y-5">
 {errorMessage && (
 <div className="p-3 bg-white border-2 border-neutral-950 rounded-md flex items-center gap-2 text-xs text-neutral-950">
 <AlertCircle className="w-4 h-4 shrink-0" />
 <span>{errorMessage}</span>
 </div>
 )}

 <>
 <div>
 <label className="text-xs font-semibold text-neutral-700 block mb-2">
 Seleccionar Proveedor
 </label>
 <div className="grid grid-cols-3 gap-2">
 {(["Telegram", "WhatsApp", "Google"] as const).map((prov) => (
 <button
 key={prov}
 type="button"
 disabled={prov === "Google"}
 onClick={() => setSelectedProvider(prov)}
 className={`p-3 rounded-xl border text-xs font-semibold flex flex-col items-center gap-1.5 transition-all ${
 selectedProvider === prov
 ? "border-neutral-950 bg-neutral-100 text-neutral-800"
 : "border-neutral-200 bg-neutral-50 text-neutral-600 hover:border-neutral-300"
 }`}
 >
 {prov === "WhatsApp" ? <QrCode className="w-4 h-4" /> : <Key className="w-4 h-4" />}
 <span>{prov}{prov === "Google" ? " (OAuth próximamente)" : ""}</span>
 </button>
 ))}
 </div>
 </div>

 {selectedProvider !== "Google" && (
 <div className="space-y-2">
 <label className="text-xs font-semibold text-neutral-700 block">
 Nombre visible
 </label>
 <input
 type="text"
 required
 value={displayName}
 onChange={(e) => setDisplayName(e.target.value)}
 placeholder={
 selectedProvider === "Telegram"
 ? "Bot de soporte"
 : "WhatsApp Ventas"
 }
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs text-neutral-950 placeholder:text-neutral-500 focus:outline-none focus:ring-1 focus:ring-neutral-950"
 />
 </div>
 )}

 {selectedProvider === "Telegram" && (
 <div className="space-y-2">
 <label className="text-xs font-semibold text-neutral-700 block">
 Bot API Token (de @BotFather)
 </label>
 <input
 type="password"
 required
 placeholder="123456789:ABCdefGHIjklMNOpqrsTUVwxyZ"
 value={botToken}
 onChange={(e) => setBotToken(e.target.value)}
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs text-neutral-950 placeholder:text-neutral-500 focus:outline-none focus:ring-1 focus:ring-neutral-950 font-mono"
 />
 <p className="text-[10px] text-neutral-500">
 Las credenciales serán cifradas inmediatamente en el Credential Vault con AES-256.
 </p>
 </div>
 )}

 {selectedProvider === "WhatsApp" && (
 <div className="space-y-4">
 <div className="space-y-2">
 <label className="text-xs font-semibold text-neutral-700 block">
 Phone Number ID
 </label>
 <input
 type="text"
 required
 value={phoneNumberId}
 onChange={(e) => setPhoneNumberId(e.target.value)}
 placeholder="Meta Phone Number ID"
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs text-neutral-950 placeholder:text-neutral-500 focus:outline-none focus:ring-1 focus:ring-neutral-950 font-mono"
 />
 </div>
 <div className="space-y-2">
 <label className="text-xs font-semibold text-neutral-700 block">
 API Token
 </label>
 <input
 type="password"
 required
 value={apiToken}
 onChange={(e) => setApiToken(e.target.value)}
 placeholder="Token permanente de Meta"
 className="w-full bg-neutral-100 border border-neutral-200 rounded-lg px-3 py-2 text-xs text-neutral-950 placeholder:text-neutral-500 focus:outline-none focus:ring-1 focus:ring-neutral-950 font-mono"
 />
 </div>
 <p className="text-[10px] text-neutral-500">
 La conexión usa credenciales reales de WhatsApp Cloud API; no se genera un QR simulado.
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
 disabled={connectMutation.isPending || selectedProvider === "Google"}
 className="px-4 py-2 rounded-lg bg-neutral-950 hover:bg-neutral-900 text-white text-xs font-semibold shadow-md transition-colors flex items-center gap-1.5 disabled:opacity-50"
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
