import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { TeamMember } from "../api/useTeam";
import { TeamMemberRow } from "./TeamMemberRow";

const mutation = () => ({ mutate: vi.fn(), isPending: false, error: null });
const actions = { changeRole: mutation(), setActive: mutation(), unlock: mutation(), passwordLink: mutation() };
vi.mock("../api/useTeam", () => ({ useTeamActions: () => actions, absoluteLink: (l: { path: string }) => l.path }));

const member = (overrides: Partial<TeamMember> = {}): TeamMember => ({
  id: "u1",
  email: "ana@ocap.io",
  fullName: "Ana",
  role: "Operator",
  isActive: true,
  isLocked: false,
  invitationPending: false,
  mfaEnabled: false,
  createdAtUtc: "",
  ...overrides,
});

describe("TeamMemberRow", () => {
  afterEach(cleanup);

  it("en la propia cuenta no ofrece cambiar el rol ni desactivarla", () => {
    render(<TeamMemberRow member={member({ role: "Admin" })} isSelf />);

    expect(screen.getByText("(tú)")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /gestionar/i })).not.toBeInTheDocument();
  });

  it("desactivar pide confirmación antes de llamar a la API", async () => {
    const user = userEvent.setup();
    render(<TeamMemberRow member={member()} isSelf={false} />);

    await user.click(screen.getByRole("button", { name: /gestionar a ana/i }));
    await user.click(screen.getByRole("button", { name: /desactivar/i }));
    expect(actions.setActive.mutate).not.toHaveBeenCalled();
    expect(screen.getByText(/se cerrarán sus sesiones/i)).toBeInTheDocument();

    await user.click(screen.getAllByRole("button", { name: "Desactivar" }).at(-1)!);
    expect(actions.setActive.mutate).toHaveBeenCalledWith({ id: "u1", active: false }, expect.anything());
  });

  it("muestra el desbloqueo solo si la cuenta está bloqueada", async () => {
    const user = userEvent.setup();
    render(<TeamMemberRow member={member({ isLocked: true })} isSelf={false} />);
    await user.click(screen.getByRole("button", { name: /gestionar a ana/i }));
    expect(screen.getByRole("button", { name: /desbloquear/i })).toBeInTheDocument();
  });
});
