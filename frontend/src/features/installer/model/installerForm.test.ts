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
    expect(validateInstallerStep("network", form)).toBeNull();
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
    form.adminEmail = "a@b.com";
    form.adminPassword = "Password_12345";
    form.googleClientId = "cid";
    form.googleClientSecret = "sec";
    form.aiApiKey = "sk";
    const payload = toSetupPayload(form);
    expect(payload.googleRedirectUri).toBe(
      "http://localhost:5000/api/integrations/Google/connect"
    );
    expect(payload.target).toBe("Local");
    expect(payload.frontendHostPort).toBe(3000);
    expect(payload.apiHostPort).toBe(5000);
  });
});
