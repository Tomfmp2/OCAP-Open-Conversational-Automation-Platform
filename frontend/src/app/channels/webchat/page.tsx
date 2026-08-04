"use client";

import React from "react";
import { MessageSquare, Send, Loader2, RefreshCw } from "lucide-react";
import { apiClient } from "@/shared/api/apiClient";
import { PageHeader } from "@/shared/components/ui/PageHeader";
import { Button } from "@/shared/components/ui/Button";
import { Surface } from "@/shared/components/ui/Surface";

interface ChatLine {
  id: string;
  role: "user" | "assistant";
  text: string;
}

function newSessionId() {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return `webchat-${Date.now()}`;
}

export default function WebChatPage() {
  const [sessionId, setSessionId] = React.useState(newSessionId);
  const [input, setInput] = React.useState("");
  const [lines, setLines] = React.useState<ChatLine[]>([]);
  const [pending, setPending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const bottomRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [lines, pending]);

  const resetSession = () => {
    setSessionId(newSessionId());
    setLines([]);
    setError(null);
  };

  const send = async (e: React.FormEvent) => {
    e.preventDefault();
    const text = input.trim();
    if (!text || pending) return;

    setInput("");
    setError(null);
    setLines((prev) => [...prev, { id: `u-${Date.now()}`, role: "user", text }]);
    setPending(true);

    try {
      const res = await apiClient.post<{
        success?: boolean;
        data?: { reply?: string };
      }>("/api/channels/webchat/messages", {
        sessionId,
        message: text,
        displayName: "Operador WebChat",
      });

      const reply = res?.data?.reply || "Sin respuesta del asistente.";

      setLines((prev) => [
        ...prev,
        { id: `a-${Date.now()}`, role: "assistant", text: String(reply) },
      ]);
    } catch (err: unknown) {
      const msg =
        err instanceof Error
          ? err.message
          : "Error al enviar. Conecta primero un canal WebChat desde Canales.";
      setError(msg);
    } finally {
      setPending(false);
    }
  };

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <PageHeader
        title="WebChat"
        description="Widget de prueba del canal embebible. Usa el Enterprise Assistant Agent."
        icon={<MessageSquare className="h-5 w-5 text-neutral-700" />}
        actions={
          <Button variant="secondary" size="sm" onClick={resetSession}>
            <RefreshCw className="h-3.5 w-3.5" />
            Nueva sesión
          </Button>
        }
      />

      <Surface className="flex flex-col h-[560px]">
        <div className="border-b border-neutral-100 px-4 py-2 text-[11px] font-mono text-neutral-500">
          session · {sessionId}
        </div>
        <div className="flex-1 overflow-y-auto space-y-3 p-4">
          {lines.length === 0 && (
            <p className="text-xs text-neutral-500">
              Escribe un mensaje para hablar con el asistente. Si no hay proveedor de IA
              configurado, recibirás una respuesta de respaldo.
            </p>
          )}
          {lines.map((line) => (
            <div
              key={line.id}
              className={`max-w-[85%] rounded-xl px-3 py-2 text-xs ${
                line.role === "user"
                  ? "ml-auto bg-neutral-950 text-white"
                  : "bg-neutral-100 text-neutral-900 border border-neutral-200"
              }`}
            >
              {line.text}
            </div>
          ))}
          {pending && (
            <div className="flex items-center gap-2 text-xs text-neutral-500">
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
              Pensando…
            </div>
          )}
          <div ref={bottomRef} />
        </div>

        {error && (
          <div className="mx-4 mb-2 rounded-md border border-neutral-950 px-3 py-2 text-xs text-neutral-950">
            {error}
          </div>
        )}

        <form onSubmit={(e) => void send(e)} className="border-t border-neutral-100 p-3 flex gap-2">
          <input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Escribe un mensaje…"
            className="flex-1 rounded-lg border border-neutral-200 bg-neutral-50 px-3 py-2 text-xs focus:outline-none focus:ring-1 focus:ring-neutral-950"
          />
          <Button type="submit" size="sm" disabled={pending || !input.trim()} loading={pending}>
            <Send className="h-3.5 w-3.5" />
            Enviar
          </Button>
        </form>
      </Surface>
    </div>
  );
}
