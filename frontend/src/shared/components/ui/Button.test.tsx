import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Button } from "./Button";

describe("Button", () => {
  it("ejecuta la acción cuando está habilitado", async () => {
    const onClick = vi.fn();
    const user = userEvent.setup();

    render(<Button onClick={onClick}>Guardar</Button>);
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    expect(onClick).toHaveBeenCalledOnce();
  });

  it("bloquea la acción durante loading", async () => {
    const onClick = vi.fn();
    const user = userEvent.setup();

    render(
      <Button loading onClick={onClick}>
        Guardando
      </Button>
    );
    const button = screen.getByRole("button", { name: "Guardando" });

    expect(button).toBeDisabled();
    expect(button).toHaveAttribute("aria-busy", "true");
    await user.click(button);
    expect(onClick).not.toHaveBeenCalled();
  });
});
