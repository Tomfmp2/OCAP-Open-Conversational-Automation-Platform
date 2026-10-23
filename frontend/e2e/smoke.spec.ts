import { expect, test, type Page } from "@playwright/test";

/**
 * Smoke del camino crítico contra un stack levantado con Docker Compose.
 * La instalación debe tener un admin: configuración inicial hecha en /setup o admin por entorno
 * (BOOTSTRAP_ADMIN_EMAIL / BOOTSTRAP_ADMIN_PASSWORD). Sus credenciales llegan por OCAP_ADMIN_EMAIL / OCAP_ADMIN_PASSWORD.
 */
const email = process.env.OCAP_ADMIN_EMAIL ?? "";
const password = process.env.OCAP_ADMIN_PASSWORD ?? "";

test.beforeAll(() => {
  if (!email || !password) {
    throw new Error("Define OCAP_ADMIN_EMAIL y OCAP_ADMIN_PASSWORD con la cuenta de administrador de la instalación.");
  }
});

async function login(page: Page) {
  await page.goto("/login");
  await expect(page.getByRole("heading", { name: "Iniciar sesión" })).toBeVisible();
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Contraseña").fill(password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await expect(page).toHaveURL(/\/$/);
}

test("login, navegación principal y logout", async ({ page }) => {
  await login(page);
  await expect(page.getByRole("heading", { name: /En qué puedo ayudarte/i })).toBeVisible();

  for (const route of ["/channels", "/channels/webchat", "/intelligence", "/agents", "/knowledge", "/settings"]) {
    await page.goto(route);
    await expect(page).toHaveURL(new RegExp(`${route.replace(/\//g, "\\/")}$`));
    await expect(page.locator("main")).toBeVisible();
  }

  await page.getByTitle("Cerrar sesión").click();
  await expect(page).toHaveURL(/\/login$/);
});

test("credenciales incorrectas muestran el error sin entrar", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Contraseña").fill(`${password}-incorrecta`);
  await page.getByRole("button", { name: "Entrar" }).click();
  // Next.js añade su propio role="alert" (anunciador de rutas): se busca el mensaje concreto.
  await expect(page.getByRole("alert").filter({ hasText: "Email o contraseña incorrectos" })).toBeVisible();
  await expect(page).toHaveURL(/\/login$/);
});

test("en una instalación configurada, /setup lleva al login", async ({ page }) => {
  await page.goto("/setup");
  await expect(page).toHaveURL(/\/login$/);
});

test("rutas de workflows redirigen al asistente (fuera de v1)", async ({ page }) => {
  await login(page);

  await page.goto("/workflows");
  await expect(page).toHaveURL(/\/$/);

  await page.goto("/workflows/designer");
  await expect(page).toHaveURL(/\/$/);
});
