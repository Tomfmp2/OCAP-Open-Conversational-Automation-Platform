import { afterEach, describe, expect, it } from "vitest";
import { clearAuthSession, getAccessToken, getTenantId, setAuthSession } from "./tokenStorage";

const user = {
  id: "u1",
  email: "admin@ocap.io",
  fullName: "Admin",
  tenantId: "t1",
  roleName: "Admin",
  permissions: [],
};

describe("tokenStorage", () => {
  afterEach(() => {
    clearAuthSession();
    localStorage.clear();
    sessionStorage.clear();
  });

  it("guarda la sesión solo en memoria, nunca en el almacenamiento del navegador", () => {
    setAuthSession("jwt-de-acceso", user);

    expect(getAccessToken()).toBe("jwt-de-acceso");
    expect(getTenantId()).toBe("t1");
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  it("al cerrar la sesión no queda nada", () => {
    setAuthSession("jwt-de-acceso", user);
    clearAuthSession();

    expect(getAccessToken()).toBeNull();
    expect(getTenantId()).toBeNull();
  });
});
