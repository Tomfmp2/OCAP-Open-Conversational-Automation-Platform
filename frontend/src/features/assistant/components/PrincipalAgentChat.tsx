"use client";

import React from "react";
import { BookOpen, Cpu, Mail, MessageSquare, RotateCcw } from "lucide-react";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { usePrincipalAgentChat } from "../api/usePrincipalAgentChat";
import { AssistantOrb, type OrbState } from "./AssistantOrb";
import { ChatComposer } from "./ChatComposer";
import { ChatMessages } from "./ChatMessages";

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
  const { lines, pending, error, providerUsed, revealingId, finishReveal, send, reset } = usePrincipalAgentChat();
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

  const orbState: OrbState = pending ? "thinking" : revealingId ? "speaking" : "idle";

  React.useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [lines, pending, revealingId]);

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
      onReset={isEmpty || pending ? undefined : resetSession}
      pending={pending}
      description={description}
      providerUsed={providerUsed}
      expanded={isEmpty}
      inputRef={inputRef}
    />
  );

  const errorBox = error && (
    <p role="alert" className="w-full rounded-2xl border border-neutral-950 bg-white px-4 py-2.5 text-left text-xs text-neutral-950">
      {error}
    </p>
  );

  if (hero && isEmpty) {
    return (
      <div
        className={`relative flex min-h-0 flex-1 flex-col items-center justify-center overflow-hidden overflow-y-auto rounded-[2rem] border border-neutral-200/70 px-4 py-10 shadow-[0_1px_3px_rgba(10,10,10,0.04)] ${className}`}
      >
        <div className="ocap-hero-ambient pointer-events-none absolute inset-0" aria-hidden />

        <div className="ocap-hero-enter relative z-[1] flex w-full max-w-2xl flex-col items-center text-center">
          <AssistantOrb size={120} state={orbState} className="ocap-mark mb-10" />

          <p className="text-sm font-medium text-neutral-500">
            {greeting}, {displayName}
          </p>
          <h1 className="mt-2 text-3xl font-semibold tracking-tight text-neutral-950 sm:text-[2.6rem] sm:leading-tight">
            ¿En qué puedo <span className="text-neutral-500">ayudarte hoy?</span>
          </h1>
          <p className="mt-3 max-w-md text-[15px] leading-relaxed text-neutral-500">
            {title === "OCAP" ? "Soy el agente principal de OCAP" : title}: conozco tus canales, tu IA, tu correo y tu
            base de conocimiento.
          </p>

          <div className="mt-10 w-full">{composer}</div>
          {errorBox && <div className="mt-3 w-full">{errorBox}</div>}

          <ul className="mt-5 flex flex-wrap items-center justify-center gap-2" aria-label="Sugerencias">
            {SUGGESTIONS.map(({ label, prompt, icon: Icon }) => (
              <li key={label}>
                <button
                  type="button"
                  onClick={() => sendText(prompt)}
                  className="focus-ring inline-flex items-center gap-1.5 rounded-full border border-neutral-200 bg-white/80 px-3.5 py-1.5 text-xs font-medium text-neutral-700 shadow-[0_1px_2px_rgba(10,10,10,0.04)] backdrop-blur transition-colors hover:border-neutral-400 hover:text-neutral-950"
                >
                  <Icon className="h-3.5 w-3.5 text-neutral-500" aria-hidden />
                  {label}
                </button>
              </li>
            ))}
          </ul>
        </div>
      </div>
    );
  }

  return (
    <div className={`flex min-h-0 flex-1 flex-col ${className}`}>
      <header className="mx-auto mb-2 flex w-full max-w-3xl items-center justify-between gap-3 px-1">
        <div className="flex items-center gap-3">
          <AssistantOrb size={36} state={orbState} />
          <div>
            <h1 className="text-sm font-semibold tracking-tight text-neutral-950">{title}</h1>
            <p className="text-xs text-neutral-500" aria-live="polite">
              {pending ? "Pensando…" : revealingId ? "Respondiendo…" : description}
            </p>
          </div>
        </div>
        <button
          type="button"
          onClick={resetSession}
          disabled={pending}
          className="focus-ring inline-flex shrink-0 items-center gap-1.5 rounded-full border border-neutral-200 bg-white px-3 py-1.5 text-xs font-medium text-neutral-600 transition-colors hover:border-neutral-400 hover:text-neutral-950 disabled:opacity-40"
        >
          <RotateCcw className="h-3 w-3" aria-hidden />
          Nueva conversación
        </button>
      </header>

      <div className="min-h-0 flex-1 overflow-y-auto px-1 py-6">
        <ChatMessages lines={lines} pending={pending} revealingId={revealingId} onRevealDone={finishReveal} />
        <div ref={bottomRef} />
      </div>

      <div className="mx-auto w-full max-w-3xl space-y-2 pb-2 pt-3">
        {errorBox}
        {composer}
      </div>
    </div>
  );
}
