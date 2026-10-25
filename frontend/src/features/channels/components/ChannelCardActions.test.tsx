import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { ChannelConnection } from "../api/useChannelsData";
import { ChannelCardActions } from "./ChannelCardActions";

const rename = { mutate: vi.fn(), isPending: false, error: null };
const remove = { mutate: vi.fn(), isPending: false, error: null };
vi.mock("../api/useChannelsData", () => ({ useRenameChannel: () => rename, useDeleteChannel: () => remove }));

const channel = (provider: string): ChannelConnection => ({
  id: "ch1",
  name: "WhatsApp +573001112233",
  provider,
  status: "Connected",
  accountIdentifier: "—",
  messagesHandled24h: 0,
  lastSync: "",
  latencyMs: 0,
  agentId: null,
  aiProviderConfigId: null,
});

describe("ChannelCardActions", () => {
  afterEach(() => {
    cleanup();
    rename.mutate.mockReset();
    remove.mutate.mockReset();
  });

  it("renombra el canal", async () => {
    const user = userEvent.setup();
    render(<ChannelCardActions channel={channel("WhatsApp")} />);

    await user.click(screen.getByRole("button", { name: /editar/i }));
    const input = screen.getByLabelText("Nombre");
    await user.clear(input);
    await user.type(input, "Ventas");
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    expect(rename.mutate).toHaveBeenCalledWith({ connectionId: "ch1", displayName: "Ventas" }, expect.anything());
  });

  it("antes de eliminar un WhatsApp avisa que se cierra la sesión del teléfono", async () => {
    const user = userEvent.setup();
    render(<ChannelCardActions channel={channel("WhatsApp")} />);

    await user.click(screen.getByRole("button", { name: /eliminar whatsapp/i }));
    expect(screen.getByText(/cerrará la sesión de WhatsApp en tu teléfono/i)).toBeInTheDocument();
    expect(remove.mutate).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: /eliminar canal/i }));
    expect(remove.mutate).toHaveBeenCalledWith("ch1", expect.anything());
  });
});
