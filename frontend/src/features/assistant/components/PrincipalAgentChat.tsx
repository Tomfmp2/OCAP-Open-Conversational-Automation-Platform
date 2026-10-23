"use client";

import React from "react";
import { BookOpen, Cpu, Loader2, Mail, MessageSquare, RefreshCw } from "lucide-react";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { usePrincipalAgentChat } from "../api/usePrincipalAgentChat";
import { ChatComposer } from "./ChatComposer";

function greetingForHour(hour: number) {
  if (hour < 12) return "Buenos días";
  if (hour < 19) return "Buenas tardes";
  return "Buenas noches";
}

const SUGGESTIONS = [
  { label: "Canales", prompt: "¿Qué canales tengo conectados?", icon: MessageSquare },
  { label: "Correo", prompt: "Envía un correo de prueba a mi dirección", icon: Mail },
  { label: "IA", prompt: "¿Qué proveedor de IA estoy usando ahora?", icon: Cpu },
  { label: "Conocimiento", prompt: "¿Qué bases de conocimiento tengo?", icon: BookOpen },
] as const;

interface PrincipalAgentChatProps {
  title?: string;
  description?: string;
  className?: string;
  /** Vista centrada tipo inicio (por defecto true) */
  hero?: boolean;
}

export function PrincipalAgentChat({
  title = "OCAP",
  description = "Agente principal",
  className = "",
  hero = true,
}: PrincipalAgentChatProps) {
  const { user } = useAuth();
  const { lines, pending, error, providerUsed, send, reset } = usePrincipalAgentChat();
  const [input, setInput] = React.useState("");
  const bottomRef = React.useRef<HTMLDivElement>(null);
  const inputRef = React.useRef<HTMLTextAreaElement>(null);

  const displayName =
    user?.fullName?.split(/\s+/)[0] ||
    user?.email?.split("@")[0] ||
    "operador";

  const greeting = React.useMemo(
    () => greetingForHour(new Date().getHours()),
    []
  );

  const isEmpty = lines.length === 0 && !pending;

  React.useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [lines, pending]);

  React.useEffect(() => {
    inputRef.current?.focus();
  }, [isEmpty]);

  const resetSession = () => {
    reset();
    setInput("");
    inputRef.current?.focus();
  };

  const sendText = (text: string) => {
    if (!text.trim() || pending) return;
    setInput("");
    void send(text);
  };

  const composer = (
    <ChatComposer
      value={input}
      onChange={setInput}
      onSubmit={() => sendText(input)}
      onReset={isEmpty ? undefined : resetSession}
      pending={pending}
      description={description}
      providerUsed={providerUsed}
      expanded={isEmpty}
      inputRef={inputRef}
    />
  );

  if (hero && isEmpty) {
    return (
      <div
        className={`relative flex min-h-0 flex-1 flex-col items-center justify-center px-4 ${className}`}
      >
        <div className="ocap-hero-ambient pointer-events-none absolute inset-0" aria-hidden />

        <div className="ocap-hero-enter relative z-[1] flex w-full max-w-2xl flex-col items-center text-center">
          <div className="ocap-mark mb-8 flex h-16 w-16 items-center justify-center rounded-full bg-neutral-950 text-xl font-semibold tracking-tight text-white shadow-[0_12px_40px_rgba(0,0,0,0.18)]">
            O
          </div>

          <p className="text-sm font-medium text-neutral-500">
            {greeting}, {displayName}
          </p>
          <h1 className="mt-2 text-3xl font-semibold tracking-tight text-neutral-950 sm:text-4xl">
            ¿En qué puedo{" "}
            <span className="text-neutral-400">ayudarte hoy?</span>
          </h1>
          <p className="mt-3 max-w-md text-sm text-neutral-500">
            {title === "OCAP" ? "Agente principal de OCAP" : title}. Canales, IA, correo y
            conocimiento del sistema.
          </p>

          <div className="mt-10 w-full">{composer}</div>

          {error && (
            <div className="mt-3 w-full rounded-lg border border-neutral-950 px-3 py-2 text-left text-xs text-neutral-950">
              {error}
            </div>
          )}

          <div className="mt-5 flex flex-wrap items-center justify-center gap-2">
            {SUGGESTIONS.map(({ label, prompt, icon: Icon }) => (
              <button
                key={label}
                type="button"
                onClick={() => sendText(prompt)}
                className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-3 py-1.5 text-xs font-medium text-neutral-700 transition-colors hover:border-neutral-950 hover:text-neutral-950"
              >
                <Icon className="h-3.5 w-3.5 text-neutral-500" />
                {label}
              </button>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`flex min-h-0 flex-1 flex-col ${className}`}>
      <div className="mb-3 flex items-center justify-between gap-3">
        <div>
          <h1 className="text-base font-semibold tracking-tight text-neutral-950">{title}</h1>
          <p className="text-[11px] text-neutral-500">{description}</p>
        </div>
        <button
          type="button"
          onClick={resetSession}
          className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-2.5 py-1.5 text-[11px] font-medium text-neutral-600 hover:border-neutral-400"
        >
          <RefreshCw className="h-3 w-3" />
          Nueva conversación
        </button>
      </div>

      <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-2xl border border-neutral-200 bg-white">
        <div className="flex-1 space-y-3 overflow-y-auto px-4 py-4">
          {lines.map((line) => (
            <div
              key={line.id}
              className={`max-w-[88%] whitespace-pre-wrap rounded-2xl px-3.5 py-2.5 text-sm leading-relaxed ${
                line.role === "user"
                  ? "ml-auto bg-neutral-950 text-white"
                  : "border border-neutral-200 bg-neutral-50 text-neutral-900"
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
          <div className="mx-4 mb-2 rounded-lg border border-neutral-950 px-3 py-2 text-xs text-neutral-950">
            {error}
          </div>
        )}

        <div className="border-t border-neutral-100 p-3">{composer}</div>
      </div>
    </div>
  );
}
