import { describe, expect, it } from "vitest";
import {
  fieldErrorsFromProblem,
  normalizeSetupCode,
  toSetupPayload,
  validateAdminStep,
  type AdminStepForm,
} from "./setupForm";

const valid: AdminStepForm = {
  setupCode: "abcd-efgh-jkmn",
  organizationName: "Acme",
  adminName: "Ana Pérez",
  adminEmail: "ana@acme.io",
  adminPassword: "Segura_Clave_2026",
  confirmPassword: "Segura_Clave_2026",
};

describe("validateAdminStep", () => {
  it("acepta un formulario completo", () => {
    expect(validateAdminStep(valid)).toEqual({});
  });

  it("pide el código y explica dónde encontrarlo", () => {
    expect(validateAdminStep({ ...valid, setupCode: "" }).setupCode).toMatch(/API/);
    expect(validateAdminStep({ ...valid, setupCode: "ABC" }).setupCode).toMatch(/12 caracteres/);
  });

  it("aplica la misma política de contraseña que la API", () => {
    expect(validateAdminStep({ ...valid, adminPassword: "corta1", confirmPassword: "corta1" }).adminPassword).toMatch(/12/);
    expect(validateAdminStep({ ...valid, adminPassword: "solamenteletras", confirmPassword: "solamenteletras" }).adminPassword).toMatch(/letras y números/);
    expect(validateAdminStep({ ...valid, confirmPassword: "otra" }).confirmPassword).toMatch(/no coinciden/);
  });

  it("valida el correo y el nombre de la organización", () => {
    expect(validateAdminStep({ ...valid, adminEmail: "ana@acme" }).adminEmail).toBeDefined();
    expect(validateAdminStep({ ...valid, organizationName: "---" }).organizationName).toBeDefined();
  });
});

describe("toSetupPayload", () => {
  it("normaliza el código y no envía la confirmación", () => {
    const payload = toSetupPayload({ ...valid, adminEmail: "  ana@acme.io " });
    expect(payload).toEqual({
      setupCode: "ABCDEFGHJKMN",
      organizationName: "Acme",
      adminName: "Ana Pérez",
      adminEmail: "ana@acme.io",
      adminPassword: "Segura_Clave_2026",
    });
    expect(normalizeSetupCode(" ab-cd ")).toBe("ABCD");
  });
});

describe("fieldErrorsFromProblem", () => {
  it("lleva los errores del servidor al campo correspondiente", () => {
    const body = { errors: { AdminPassword: ["Muy débil."], Unknown: ["x"] } };
    expect(fieldErrorsFromProblem(body)).toEqual({ adminPassword: "Muy débil." });
    expect(fieldErrorsFromProblem(null)).toEqual({});
  });
});
