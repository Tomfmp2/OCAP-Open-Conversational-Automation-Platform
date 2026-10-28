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

 it("cierra la sesión sin enviar tokens: la API usa la cookie HttpOnly", async () => {
 await authApi.logout();

 expect(apiClient.post).toHaveBeenCalledWith("/api/auth/logout", undefined, { skipAuth: true, skipRetry: true });
 });
});
