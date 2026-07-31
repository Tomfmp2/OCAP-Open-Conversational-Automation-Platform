import { describe, expect, it } from "vitest";
import {
  defaultInstallerForm,
  toSetupPayload,
  validateInstallerStep,
} from "./installerForm";

describe("installerForm", () => {
  it("valida red local por puertos", () => {
    const form = defaultInstallerForm();
    form.target = "Local";
    form.frontendHostPort = 3000;
    form.apiHostPort = 5000;
    expect(validateInstallerStep("network", form)).toBeNull();
    form.frontendHostPort = 0;
    expect(validateInstallerStep("network", form)).toMatch(/Puerto/);
  });

  it("exige URLs en modo web", () => {
    const form = defaultInstallerForm();
    form.target = "Web";
    form.publicApiUrl = "not-a-url";
    form.publicPanelUrl = "https://app.example.com";
    expect(validateInstallerStep("network", form)).toMatch(/API/);
  });

  it("arma payload con redirect Google derivado", () => {
    const form = defaultInstallerForm();
    form.target = "Local";
    form.apiHostPort = 5001;
    form.adminEmail = "a@b.com";
    form.adminPassword = "Password_12345";
    form.postgresPassword = "Postgres_123";
    form.googleClientId = "cid";
    form.googleClientSecret = "sec";
    form.aiApiKey = "sk";
    const payload = toSetupPayload(form);
    expect(payload.googleRedirectUri).toBe(
      "http://localhost:5001/api/integrations/Google/connect"
    );
    expect(payload.target).toBe("Local");
  });
});
