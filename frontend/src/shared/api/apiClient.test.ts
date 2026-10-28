import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient, ApiError, refreshSession } from "./apiClient";
import { getAccessToken } from "./tokenStorage";

function respond(status: number) {
  return new Response(JSON.stringify({ title: `Error ${status}` }), {
    status,
    headers: { "content-type": "application/json" },
  });
}

describe("apiClient: reintentos", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("no reintenta un POST que falla con 503 (podría ejecutar la acción dos veces)", async () => {
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(respond(503)));
    vi.stubGlobal("fetch", fetchMock);

    await expect(apiClient.post("/api/agents/principal/messages", { message: "hola" }, { skipAuth: true })).rejects.toBeInstanceOf(ApiError);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("reintenta un GET ante un 503 transitorio", async () => {
    const fetchMock = vi
      .fn()
      .mockImplementationOnce(() => Promise.resolve(respond(503)))
      .mockImplementationOnce(() => Promise.resolve(new Response("{}", { status: 200, headers: { "content-type": "application/json" } })));
    vi.stubGlobal("fetch", fetchMock);

    await expect(apiClient.get("/api/health", { skipAuth: true })).resolves.toEqual({});
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});

describe("apiClient: sesión con cookie", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("renueva con la cookie HttpOnly, sin enviar ningún token en el cuerpo", async () => {
    const session = { accessToken: "jwt-nuevo", userId: "u1", tenantId: "t1", email: "a@b.c", roleName: "Admin" };
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(session), { status: 200, headers: { "content-type": "application/json" } })
    );
    vi.stubGlobal("fetch", fetchMock);

    await expect(refreshSession()).resolves.toEqual(session);

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/auth/refresh");
    expect(init.credentials).toBe("include");
    expect(init.body).toBeUndefined();
    expect(getAccessToken()).toBe("jwt-nuevo");
  });

  it("sin cookie válida no hay sesión", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(respond(401)));

    await expect(refreshSession()).resolves.toBeNull();
  });
});
