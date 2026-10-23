"use client";

import React from "react";
import { AlertCircle, ArrowUp, Loader2, UserRound } from "lucide-react";
import { Modal } from "@/shared/components/ui/Modal";
import { Button } from "@/shared/components/ui/Button";
import { testAgent, type AgentTestTurn } from "../api/useAgentsData";

interface AgentTestPanelProps {
  /** Agente a probar (DEFAULT_AGENT_ID para el de atención por defecto); null cierra el panel. */
  agent: { id: string; name: string } | null;
  onClose: () => void;
}

/** Chat de prueba: responde como lo haría en un canal, sin guardar la conversación ni ejecutar herramientas. */
export function AgentTestPanel({ agent, onClose }: AgentTestPanelProps) {
  return (
    <Modal
      open={agent !== null}
      onClose={onClose}
      title={agent ? `Probar ${agent.name}` : "Probar agente"}
      description="Escribe como lo haría un cliente. No se guarda nada ni se ejecutan herramientas."
      className="max-w-xl"
    >
      {agent && <TestChat key={agent.id} agentId={agent.id} />}
    </Modal>
  );
}

function TestChat({ agentId }: { agentId: string }) {
  const [turns, setTurns] = React.useState<AgentTestTurn[]>([]);
  const [input, setInput] = React.useState("");
  const [pending, setPending] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [transferred, setTransferred] = React.useState(false);
  const bottomRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    bottomRef.current?.scrollIntoView({ block: "end" });
  }, [turns, pending]);

  const send = async () => {
    const text = input.trim();
    if (!text || pending) return;
    const history = turns;
    setInput("");
    setError(null);
    setTurns([...history, { fromContact: true, text }]);
    setPending(true);
    try {
      const result = await testAgent(agentId, text, history);
      setTurns((current) => [...current, { fromContact: false, text: result.reply }]);
      if (result.transferToHuman) setTransferred(true);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "El agente no respondió.");
    } finally {
      setPending(false);
    }
  };

  return (
    <div className="space-y-3">
      <div className="h-72 space-y-2 overflow-y-auto rounded-md border border-neutral-200 bg-neutral-50 p-3" aria-live="polite">
        {turns.length === 0 && !pending && (
          <p className="pt-24 text-center text-xs text-neutral-500">Escribe el primer mensaje del cliente.</p>
        )}
        {turns.map((turn, index) => (
          <p
            key={index}
            className={`max-w-[85%] whitespace-pre-wrap rounded-2xl px-3 py-2 text-sm ${
              turn.fromContact ? "ml-auto bg-neutral-950 text-white" : "border border-neutral-200 bg-white text-neutral-900"
            }`}
          >
            {turn.text}
          </p>
        ))}
        {pending && (
          <p className="flex items-center gap-2 text-xs text-neutral-500">
            <Loader2 className="h-3.5 w-3.5 animate-spin motion-reduce:animate-none" aria-hidden /> Pensando…
          </p>
        )}
        <div ref={bottomRef} />
      </div>

      {transferred && (
        <p className="flex items-center gap-2 text-xs text-neutral-700">
          <UserRound className="h-3.5 w-3.5" aria-hidden /> En un canal real, aquí la conversación pasaría a tu equipo en la bandeja.
        </p>
      )}
      {error && (
        <p role="alert" className="flex items-center gap-2 text-xs text-neutral-950">
          <AlertCircle className="h-3.5 w-3.5" aria-hidden /> {error}
        </p>
      )}

      <form
        className="flex gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          void send();
        }}
      >
        <label htmlFor="agent-test-input" className="sr-only">
          Mensaje del cliente
        </label>
        <input
          id="agent-test-input"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="¿A qué hora abren?"
          disabled={pending}
          className="focus-ring w-full rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm"
        />
        <Button type="submit" size="sm" disabled={!input.trim()} loading={pending} aria-label="Enviar">
          <ArrowUp className="h-4 w-4" aria-hidden />
        </Button>
      </form>
    </div>
  );
}
