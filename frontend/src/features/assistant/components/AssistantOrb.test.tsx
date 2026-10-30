import { act, cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AssistantOrb } from "./AssistantOrb";
import { TypewriterText } from "./TypewriterText";

describe("AssistantOrb", () => {
  afterEach(cleanup);

  it("refleja el estado del agente y es decorativo para lectores de pantalla", () => {
    const { container } = render(<AssistantOrb state="thinking" size={40} />);
    const orb = container.firstElementChild as HTMLElement;

    expect(orb.dataset.state).toBe("thinking");
    expect(orb.getAttribute("aria-hidden")).toBe("true");
    expect(orb.style.width).toBe("40px");
  });
});

describe("TypewriterText", () => {
  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("muestra la respuesta de a poco y avisa al terminar", () => {
    vi.useFakeTimers();
    const onDone = vi.fn();
    render(<TypewriterText text="uno dos tres cuatro" onDone={onDone} />);

    // Lectores de pantalla: el texto completo desde el principio.
    expect(screen.getByText("uno dos tres cuatro", { selector: ".sr-only" })).toBeInTheDocument();
    expect(onDone).not.toHaveBeenCalled();

    // Cada paso programa el siguiente tras volver a renderizar: se avanza de a poco.
    for (let i = 0; i < 80; i++) {
      act(() => {
        vi.advanceTimersByTime(30);
      });
    }
    expect(screen.getByText("uno dos tres cuatro", { selector: "[aria-hidden]" })).toBeInTheDocument();
    expect(onDone).toHaveBeenCalledOnce();
  });

  it("con movimiento reducido muestra todo de inmediato", () => {
    vi.stubGlobal("matchMedia", () => ({ matches: true }));
    const onDone = vi.fn();
    render(<TypewriterText text="hola mundo" onDone={onDone} />);

    expect(screen.getByText("hola mundo", { selector: "[aria-hidden]" })).toBeInTheDocument();
    expect(onDone).toHaveBeenCalledOnce();
  });
});
