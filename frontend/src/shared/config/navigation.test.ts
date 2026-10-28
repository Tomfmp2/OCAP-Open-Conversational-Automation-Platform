import { describe, expect, it } from "vitest";
import { isActive, isPublicRoute, requiredPermissionFor } from "./navigation";

describe("requiredPermissionFor", () => {
  it("usa el permiso más específico de la ruta", () => {
    expect(requiredPermissionFor("/")).toBe("AI.Execute");
    expect(requiredPermissionFor("/inbox")).toBe("Conversation.Read");
    expect(requiredPermissionFor("/channels")).toBe("Channel.Manage");
    expect(requiredPermissionFor("/team")).toBe("Security.Manage");
    expect(requiredPermissionFor("/settings")).toBe("Settings.Manage");
  });

  it("Mi cuenta solo pide sesión", () => {
    expect(requiredPermissionFor("/account")).toBeUndefined();
  });

  it("reconoce las pantallas públicas", () => {
    expect(isPublicRoute("/accept")).toBe(true);
    expect(isPublicRoute("/forgot-password")).toBe(true);
    expect(isPublicRoute("/inbox")).toBe(false);
  });
});

describe("isActive", () => {
  it("el asistente solo está activo en la raíz", () => {
    expect(isActive("/", "/")).toBe(true);
    expect(isActive("/inbox", "/")).toBe(false);
  });

  it("una sección sigue activa en sus subrutas, pero no en rutas que solo empiezan igual", () => {
    expect(isActive("/channels/webchat", "/channels")).toBe(true);
    expect(isActive("/settingsx", "/settings")).toBe(false);
  });
});
