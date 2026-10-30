import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { ChannelConnection } from "../api/useChannelsData";
import { ChannelSettings } from "./ChannelSettings";

const mutation = () => ({ mutate: vi.fn(), isPending: false, error: null, data: undefined, isSuccess: false });
const rename = mutation();
const remove = mutation();
const power = mutation();
vi.mock("../api/useChannelsData", () => ({
  useRenameChannel: () => rename,
  useDeleteChannel: () => remove,
  useChannelPower: () => power,
  useConfigureWidget: () => mutation(),
  useRotateWidgetKey: () => mutation(),
  useAssignChannelAgent: () => mutation(),
  useAssignChannelAi: () => mutation(),
  useAiProviderOptions: () => ({ data: [], isLoading: false }),
}));
vi.mock("@/features/agents/api/useAgentsData", () => ({ useAgents: () => ({ data: [], isLoading: false }) }));
vi.mock("../api/useWhatsAppLink", () => ({ useWhatsAppLink: () => ({ status: { state: "linked" }, error: null, busy: false, link: vi.fn(), unlink: vi.fn() }) }));

const channel: ChannelConnection = {
  id: "ch1",
  name: "WhatsApp tienda",
  provider: "WhatsApp",
  status: "Connected",
  accountIdentifier: null,
  agentId: null,
  aiProviderConfigId: null,
  power: "on",
};

describe("ChannelSettings", () => {
  it("apagar pide el apagado gradual del canal", async () => {
    render(<ChannelSettings channel={channel} open onClose={vi.fn()} />);
    expect(screen.getByText(/Encendido: responde/)).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Apagar" }));

    expect(power.mutate).toHaveBeenCalledWith({ id: "ch1", on: false });
  });

  it("un canal apagándose explica que termina los chats en curso y se puede volver a encender", () => {
    render(<ChannelSettings channel={{ ...channel, power: "stopping" }} open onClose={vi.fn()} />);
    expect(screen.getByText(/termina los chats en curso/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Encender" })).toBeInTheDocument();
  });

  afterEach(() => {
    cleanup();
    rename.mutate.mockReset();
    remove.mutate.mockReset();
  });

  it("renombra el canal", async () => {
    const user = userEvent.setup();
    render(<ChannelSettings channel={channel} open onClose={vi.fn()} />);

    const input = screen.getByLabelText("Nombre");
    await user.clear(input);
    await user.type(input, "Ventas");
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    expect(rename.mutate).toHaveBeenCalledWith({ connectionId: "ch1", displayName: "Ventas" }, expect.anything());
  });

  it("antes de eliminar un WhatsApp avisa que se cierra la sesión del teléfono", async () => {
    const user = userEvent.setup();
    render(<ChannelSettings channel={channel} open onClose={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Eliminar canal" }));
    expect(screen.getByText(/cierra la sesión de WhatsApp en tu teléfono/i)).toBeInTheDocument();
    expect(remove.mutate).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: "Eliminar canal" }));
    expect(remove.mutate).toHaveBeenCalledWith("ch1", expect.anything());
  });
});
