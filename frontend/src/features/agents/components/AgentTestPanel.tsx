"use client";

import React from "react";
import { ArrowUp, Loader2 } from "lucide-react";
import { Button, FormError, Modal } from "@/shared/components/ui";
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
      description="Escribe como un cliente. No se guarda la conversación ni se ejecutan acciones."
      className="sm:max-w-xl"
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
      <div className="h-[min(18rem,45dvh)] space-y-2 overflow-y-auto rounded-lg border border-neutral-200 bg-neutral-50 p-3" aria-live="polite">
        {turns.length === 0 && !pending && (
          <p className="flex h-full items-center justify-center text-sm text-neutral-600">Escribe el primer mensaje.</p>
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
          <p className="flex items-center gap-2 text-sm text-neutral-600">
            <Loader2 className="h-4 w-4 animate-spin motion-reduce:animate-none" aria-hidden /> Escribiendo…
          </p>
        )}
        <div ref={bottomRef} />
      </div>

      {transferred && <p className="text-sm text-warning">Aquí la conversación pasaría a tu equipo, en Conversaciones.</p>}
      <FormError error={error ? new Error(error) : null} />

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
          className="focus-ring h-9 min-w-0 flex-1 rounded-md border border-neutral-300 bg-white px-3 text-sm"
        />
        <Button type="submit" disabled={!input.trim()} loading={pending} aria-label="Enviar">
          <ArrowUp className="h-4 w-4" aria-hidden />
        </Button>
      </form>
    </div>
  );
}
