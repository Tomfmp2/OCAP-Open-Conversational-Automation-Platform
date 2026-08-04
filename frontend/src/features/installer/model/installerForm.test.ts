import { describe, expect, it } from "vitest";
import {
  defaultInstallerForm,
  getInstallerSteps,
  parseGoogleCredentialsJson,
  toSetupPayload,
  validateInstallerStep,
} from "./installerForm";

describe("installerForm", () => {
  it("wizard Dev omite red y postgres", () => {
    const steps = getInstallerSteps("Dev");
    expect(steps.map((s) => s.id)).toEqual([
      "mode",
      "admin",
      "ai",
      "google",
      "review",
      "diagnostic",
    ]);
  });

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

  it("permite IA sin key en Dev", () => {
    const form = defaultInstallerForm();
    form.target = "Dev";
    form.aiApiKey = "";
    expect(validateInstallerStep("ai", form)).toBeNull();
  });

  it("arma payload Dev con :5229", () => {
    const form = defaultInstallerForm();
    form.target = "Dev";
    form.adminEmail = "a@b.com";
    form.adminPassword = "Password_12345";
    form.aiApiKey = "sk";
    const payload = toSetupPayload(form);
    expect(payload.googleRedirectUri).toBe(
      "http://localhost:5229/api/integrations/Google/connect"
    );
    expect(payload.target).toBe("Dev");
    expect(payload.apiHostPort).toBe(5229);
    expect(payload.publicApiUrl).toBe("http://localhost:5229");
  });

  it("arma payload Local con :5000", () => {
    const form = defaultInstallerForm();
    form.target = "Local";
    form.adminEmail = "a@b.com";
    form.adminPassword = "Password_12345";
    form.aiApiKey = "sk";
    const payload = toSetupPayload(form);
    expect(payload.apiHostPort).toBe(5000);
    expect(payload.publicApiUrl).toBe("http://localhost:5000");
  });

  it("parsea JSON OAuth de Google Cloud", () => {
    const parsed = parseGoogleCredentialsJson(
      JSON.stringify({
        web: {
          client_id: "123.apps.googleusercontent.com",
          client_secret: "GOCSPX-secret",
          redirect_uris: ["http://localhost:5229/api/integrations/Google/connect"],
        },
      })
    );
    expect(parsed).toEqual({
      clientId: "123.apps.googleusercontent.com",
      clientSecret: "GOCSPX-secret",
      redirectUri: "http://localhost:5229/api/integrations/Google/connect",
    });
  });
});
