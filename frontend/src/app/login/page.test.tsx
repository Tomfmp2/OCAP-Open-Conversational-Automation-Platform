import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import LoginPage from "./page";

const login = vi.fn();
const completeMfa = vi.fn();
vi.mock("@/features/auth/context/AuthProvider", () => ({ useAuth: () => ({ login, completeMfa }) }));
vi.mock("@/features/setup/api/setupApi", () => ({ useSetupStatus: () => ({ data: { required: false } }) }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ replace: vi.fn() }) }));

describe("LoginPage", () => {
  afterEach(() => {
    cleanup();
    login.mockReset();
    completeMfa.mockReset();
  });

  it("con MFA activo pide el código y completa el login con el desafío", async () => {
    login.mockResolvedValue({ mfaToken: "desafio" });
    const user = userEvent.setup();
    render(<LoginPage />);

    await user.type(screen.getByLabelText("Email"), "ana@ocap.io");
    await user.type(screen.getByLabelText("Contraseña"), "una-contraseña-larga");
    await user.click(screen.getByRole("button", { name: "Entrar" }));

    const code = await screen.findByLabelText("Código de verificación");
    expect(screen.queryByLabelText("Contraseña")).not.toBeInTheDocument();
    await user.type(code, "123456");
    await user.click(screen.getByRole("button", { name: "Verificar" }));

    expect(completeMfa).toHaveBeenCalledWith("desafio", "123456");
  });

  it("sin MFA no muestra el paso del código", async () => {
    login.mockResolvedValue(null);
    const user = userEvent.setup();
    render(<LoginPage />);

    await user.type(screen.getByLabelText("Email"), "ana@ocap.io");
    await user.type(screen.getByLabelText("Contraseña"), "una-contraseña-larga");
    await user.click(screen.getByRole("button", { name: "Entrar" }));

    expect(screen.queryByLabelText("Código de verificación")).not.toBeInTheDocument();
    expect(completeMfa).not.toHaveBeenCalled();
  });
});
