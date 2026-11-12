import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TimeZonePrompt } from "./TimeZonePrompt";

const mutate = vi.fn();
let timeZone: string | null = null;
let canManage = true;

vi.mock("@/features/auth/context/AuthProvider", () => ({
  useAuth: () => ({ hasPermission: () => canManage }),
}));
vi.mock("@/features/settings/api/useOrganization", () => ({
  usePreferences: () => ({ data: { language: "es", timeZone, panelUrl: null, conversationRetentionDays: null, memoryDays: null, privacyNotice: null } }),
  useSavePreferences: () => ({ mutate, isPending: false, error: null }),
}));

describe("TimeZonePrompt", () => {
  afterEach(cleanup);

  beforeEach(() => {
    mutate.mockReset();
    timeZone = null;
    canManage = true;
  });

  it("propone la zona del equipo y solo la guarda si la persona acepta", () => {
    render(<TimeZonePrompt />);
    const zone = Intl.DateTimeFormat().resolvedOptions().timeZone;

    expect(mutate).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: `Usar ${zone}` }));

    expect(mutate).toHaveBeenCalledWith(expect.objectContaining({ timeZone: zone, language: "es" }));
  });

  it("«Ahora no» lo oculta sin guardar nada", () => {
    render(<TimeZonePrompt />);
    fireEvent.click(screen.getByRole("button", { name: "Ahora no" }));

    expect(screen.queryByRole("status")).toBeNull();
    expect(mutate).not.toHaveBeenCalled();
  });

  it("no aparece si la organización ya tiene zona horaria", () => {
    timeZone = "America/Bogota";
    render(<TimeZonePrompt />);

    expect(screen.queryByRole("status")).toBeNull();
  });

  it("quien no administra ve a quién pedírselo, sin botones", () => {
    canManage = false;
    render(<TimeZonePrompt />);

    expect(screen.getByRole("status").textContent).toContain("administrador");
    expect(screen.queryByRole("button")).toBeNull();
  });
});
