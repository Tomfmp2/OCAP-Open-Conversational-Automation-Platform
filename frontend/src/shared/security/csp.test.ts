import { describe, expect, it } from "vitest";
import { buildContentSecurityPolicy } from "./csp";

describe("buildContentSecurityPolicy", () => {
  it("solo permite scripts con el nonce y prohíbe incrustar el panel", () => {
    const csp = buildContentSecurityPolicy("abc", { isDev: false });

    expect(csp).toContain("script-src 'self' 'nonce-abc' 'strict-dynamic'");
    expect(csp).not.toContain("unsafe-eval");
    expect(csp).not.toMatch(/script-src[^;]*unsafe-inline/);
    expect(csp).toContain("frame-ancestors 'none'");
    expect(csp).toContain("object-src 'none'");
  });

  it("permite conectar con la API externa por http y websocket", () => {
    const csp = buildContentSecurityPolicy("abc", { isDev: true, apiUrl: "http://localhost:5229/" });

    expect(csp).toContain("connect-src 'self' http://localhost:5229 ws://localhost:5229");
    expect(csp).toContain("'unsafe-eval'");
  });
});
