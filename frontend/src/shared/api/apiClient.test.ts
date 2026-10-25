import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient, ApiError } from "./apiClient";

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
