import { describe, expect, it } from "vitest";
import { isPublicRoute, requiredPermissionFor } from "./navigation";

describe("requiredPermissionFor", () => {
  it("usa el permiso más específico de la ruta", () => {
    expect(requiredPermissionFor("/")).toBe("AI.Execute");
    expect(requiredPermissionFor("/inbox")).toBe("Conversation.Read");
    expect(requiredPermissionFor("/channels")).toBe("Channel.Manage");
    expect(requiredPermissionFor("/channels/webchat")).toBe("Channel.Manage");
    expect(requiredPermissionFor("/team")).toBe("Security.Manage");
  });

  it("Mi cuenta y Ajustes solo piden sesión", () => {
    expect(requiredPermissionFor("/account")).toBeUndefined();
    expect(requiredPermissionFor("/settings")).toBeUndefined();
  });

  it("reconoce las pantallas públicas", () => {
    expect(isPublicRoute("/accept")).toBe(true);
    expect(isPublicRoute("/forgot-password")).toBe(true);
    expect(isPublicRoute("/inbox")).toBe(false);
  });
});
