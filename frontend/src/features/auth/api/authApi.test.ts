import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/shared/api/apiClient";
import { authApi } from "./authApi";

vi.mock("@/shared/api/apiClient", () => ({
 apiClient: {
 post: vi.fn(),
 get: vi.fn(),
 },
}));

describe("authApi", () => {
 beforeEach(() => {
 vi.mocked(apiClient.post).mockResolvedValue({ message: "ok" });
 });

 it("envía el refresh token al cerrar sesión", async () => {
 await authApi.logout("refresh-token-value");

 expect(apiClient.post).toHaveBeenCalledWith("/api/auth/logout", {
 refreshToken: "refresh-token-value",
 });
 });

 it("no inventa un token cuando no está disponible", async () => {
 await authApi.logout(null);

 expect(apiClient.post).toHaveBeenCalledWith("/api/auth/logout", {
 refreshToken: undefined,
 });
 });
});
