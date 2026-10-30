import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { WhatsAppLinkStatus } from "../api/useWhatsAppLink";
import { WhatsAppLinkPanel } from "./WhatsAppLinkPanel";

const hook = {
  status: null as WhatsAppLinkStatus | null,
  error: null as string | null,
  busy: false,
  live: true,
  link: vi.fn(),
  unlink: vi.fn(),
};

vi.mock("../api/useWhatsAppLink", () => ({ useWhatsAppLink: () => hook }));

describe("WhatsAppLinkPanel", () => {
  afterEach(cleanup);

  beforeEach(() => {
    hook.status = null;
    hook.error = null;
    hook.live = true;
    hook.link.mockReset();
    hook.unlink.mockReset();
  });

  it("sin vincular ofrece generar el QR", async () => {
    hook.status = { state: "unlinked" };
    const user = userEvent.setup();

    render(<WhatsAppLinkPanel active onLinked={vi.fn()} />);
    await user.click(screen.getByRole("button", { name: /generar qr/i }));

    expect(hook.link).toHaveBeenCalledOnce();
  });

  it("pendiente muestra el QR para escanear", () => {
    hook.status = { state: "pending", qrBase64: "data:image/png;base64,QR" };

    render(<WhatsAppLinkPanel active onLinked={vi.fn()} />);

    expect(screen.getByRole("img", { name: /código qr/i })).toHaveAttribute("src", "data:image/png;base64,QR");
    expect(screen.getByText(/esperando el escaneo/i)).toBeInTheDocument();
  });

  it("vinculado muestra el número y permite desvincular", async () => {
    hook.status = { state: "linked", phoneNumber: "573001234567" };
    const user = userEvent.setup();

    render(<WhatsAppLinkPanel active onLinked={vi.fn()} />);
    await user.click(screen.getByRole("button", { name: /desvincular/i }));

    expect(screen.getByText(/\+573001234567/)).toBeInTheDocument();
    expect(hook.unlink).toHaveBeenCalledOnce();
  });

  it("muestra el error del servicio", () => {
    hook.error = "No se pudo contactar el servicio de WhatsApp";

    render(<WhatsAppLinkPanel active onLinked={vi.fn()} />);

    expect(screen.getByRole("alert")).toHaveTextContent("No se pudo contactar el servicio de WhatsApp");
  });
});
