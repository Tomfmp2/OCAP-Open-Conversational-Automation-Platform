import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ChannelAgentSelect } from "./ChannelAgentSelect";

const assign = { mutate: vi.fn(), isPending: false, isError: false, error: null };

vi.mock("@/features/agents/api/useAgentsData", () => ({
  useAgents: () => ({ isLoading: false, data: [{ id: "a1", name: "Ventas" }] }),
}));
vi.mock("../api/useChannelsData", () => ({ useAssignChannelAgent: () => assign }));

describe("ChannelAgentSelect", () => {
  afterEach(() => {
    cleanup();
    assign.mutate.mockReset();
  });

  it("asigna el agente elegido y permite volver al de atención por defecto", async () => {
    const user = userEvent.setup();
    render(<ChannelAgentSelect connectionId="c1" agentId={null} />);

    await user.selectOptions(screen.getByLabelText("Agente que responde"), "a1");
    expect(assign.mutate).toHaveBeenCalledWith({ connectionId: "c1", agentId: "a1" });
  });

  it("con agente asignado, la opción vacía lo quita", async () => {
    const user = userEvent.setup();
    render(<ChannelAgentSelect connectionId="c1" agentId="a1" />);

    await user.selectOptions(screen.getByLabelText("Agente que responde"), "");
    expect(assign.mutate).toHaveBeenCalledWith({ connectionId: "c1", agentId: null });
  });
});
