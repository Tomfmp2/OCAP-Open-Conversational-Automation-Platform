import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ChannelAiSelect } from "./ChannelAiSelect";

const assign = { mutate: vi.fn(), isPending: false, isError: false, error: null };
const options = {
  isLoading: false,
  data: [
    { id: "g1", label: "Gemini · gemini-3.5-flash", enabled: true },
    { id: "c1", label: "Claude · claude-sonnet-5-5", enabled: false },
  ],
};

vi.mock("../api/useChannelsData", () => ({
  useAiProviderOptions: () => options,
  useAssignChannelAi: () => assign,
}));
vi.mock("next/link", () => ({ default: ({ children }: { children: React.ReactNode }) => <a>{children}</a> }));

describe("ChannelAiSelect", () => {
  afterEach(() => {
    cleanup();
    assign.mutate.mockReset();
  });

  it("asigna un proveedor habilitado y oculta los deshabilitados no asignados", async () => {
    const user = userEvent.setup();
    render(<ChannelAiSelect connectionId="ch1" aiProviderConfigId={null} />);

    expect(screen.queryByRole("option", { name: /Claude/ })).not.toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText("Modelo de IA"), "g1");
    expect(assign.mutate).toHaveBeenCalledWith({ connectionId: "ch1", aiProviderConfigId: "g1" });
  });

  it("vuelve a la IA de la organización con la opción vacía", async () => {
    const user = userEvent.setup();
    render(<ChannelAiSelect connectionId="ch1" aiProviderConfigId="g1" />);

    await user.selectOptions(screen.getByLabelText("Modelo de IA"), "");
    expect(assign.mutate).toHaveBeenCalledWith({ connectionId: "ch1", aiProviderConfigId: null });
  });
});
