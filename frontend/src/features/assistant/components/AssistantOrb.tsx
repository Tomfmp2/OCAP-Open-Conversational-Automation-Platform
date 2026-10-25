import type { CSSProperties } from "react";
import { cn } from "@/shared/utils/cn";

export type OrbState = "idle" | "thinking" | "speaking";

interface AssistantOrbProps {
  state?: OrbState;
  /** Diámetro en píxeles. */
  size?: number;
  className?: string;
}

/**
 * Orbe de aurora del asistente: respira en reposo, gira más rápido mientras piensa y late mientras responde.
 * Es decorativo (aria-hidden): el estado se anuncia con texto aparte para los lectores de pantalla.
 */
export function AssistantOrb({ state = "idle", size = 112, className }: AssistantOrbProps) {
  return (
    <div
      aria-hidden
      data-state={state}
      className={cn("ocap-orb", className)}
      style={{ width: size, height: size, "--orb-size": `${size}px` } as CSSProperties}
    >
      <div className="ocap-orb__halo" />
      <div className="ocap-orb__body">
        <div className="ocap-orb__layer ocap-orb__layer--red" />
        <div className="ocap-orb__layer ocap-orb__layer--amber" />
        <div className="ocap-orb__layer ocap-orb__layer--pink" />
        <div className="ocap-orb__layer ocap-orb__layer--swirl" />
      </div>
    </div>
  );
}
