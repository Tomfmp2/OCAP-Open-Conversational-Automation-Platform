import { readFileSync } from "node:fs";
import { join } from "node:path";
import { expect, test, type Page } from "@playwright/test";

/**
 * Accesibilidad automática (axe-core, WCAG 2.1 A/AA) en las páginas principales del panel. Falla con violaciones
 * graves o críticas. axe detecta solo una parte de los problemas: no reemplaza la revisión con teclado y lector.
 */
const email = process.env.OCAP_ADMIN_EMAIL ?? "";
const password = process.env.OCAP_ADMIN_PASSWORD ?? "";
const axeSource = readFileSync(join(process.cwd(), "node_modules/axe-core/axe.min.js"), "utf8");

// axe se inyecta como script: la CSP con nonce del panel lo bloquearía.
test.use({ bypassCSP: true });

type Violation = { id: string; impact: string | null; help: string; nodes: { target: string[] }[] };

async function seriousViolations(page: Page): Promise<string[]> {
  await page.addScriptTag({ content: axeSource });
  const violations = await page.evaluate(async () => {
    const axe = (window as unknown as { axe: { run: (ctx: Document, opts: object) => Promise<{ violations: Violation[] }> } }).axe;
    const result = await axe.run(document, { runOnly: { type: "tag", values: ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } });
    return result.violations;
  });
  return violations
    .filter((v) => v.impact === "serious" || v.impact === "critical")
    .map((v) => `${v.id} (${v.impact}): ${v.help} → ${v.nodes.slice(0, 3).map((n) => n.target.join(" ")).join(", ")}`);
}

test("login sin violaciones WCAG graves", async ({ page }) => {
  await page.goto("/login");
  expect(await seriousViolations(page)).toEqual([]);
});

test("páginas del panel sin violaciones WCAG graves", async ({ page }) => {
  test.skip(!email || !password, "Define OCAP_ADMIN_EMAIL y OCAP_ADMIN_PASSWORD.");
  // Recorre nueve páginas y analiza cada una: el límite por defecto (30 s) no alcanza en móvil con la suite en paralelo.
  test.setTimeout(120_000);
  await page.goto("/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Contraseña").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await expect(page).toHaveURL(/\/$/);

  const found: Record<string, string[]> = {};
  for (const route of ["/", "/inbox", "/channels", "/agents", "/knowledge", "/intelligence", "/team", "/settings", "/account"]) {
    await page.goto(route);
    await page.waitForLoadState("networkidle");
    const violations = await seriousViolations(page);
    if (violations.length) found[route] = violations;
  }

  expect(found).toEqual({});
});
