"use client";

import React from "react";

interface TypewriterTextProps {
  text: string;
  /** Se llama cuando el texto termina de aparecer (o de inmediato con movimiento reducido). */
  onDone?: () => void;
}

/** Duración máxima del efecto: una respuesta larga no debe hacer esperar al usuario. */
const MAX_DURATION_MS = 1600;
const TICK_MS = 30;

function prefersReducedMotion() {
  return typeof window !== "undefined" && window.matchMedia?.("(prefers-reduced-motion: reduce)").matches === true;
}

/** Muestra la respuesta palabra por palabra, mientras el orbe "habla". */
export function TypewriterText({ text, onDone }: TypewriterTextProps) {
  const words = React.useMemo(() => text.split(/(\s+)/), [text]);
  const [shown, setShown] = React.useState(() => (prefersReducedMotion() ? words.length : 0));
  const doneRef = React.useRef(onDone);
  React.useEffect(() => {
    doneRef.current = onDone;
  });

  React.useEffect(() => {
    if (shown >= words.length) {
      doneRef.current?.();
      return;
    }
    const perTick = Math.max(1, Math.ceil(words.length / (MAX_DURATION_MS / TICK_MS)));
    const timer = window.setTimeout(() => setShown((n) => Math.min(words.length, n + perTick)), TICK_MS);
    return () => window.clearTimeout(timer);
  }, [shown, words.length]);

  return (
    <>
      {/* Para lectores de pantalla, la respuesta completa de una vez. */}
      <span className="sr-only">{text}</span>
      <span aria-hidden>{words.slice(0, shown).join("")}</span>
    </>
  );
}
