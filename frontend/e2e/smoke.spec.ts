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

  for (const route of ["/inbox", "/channels", "/channels/webchat", "/intelligence", "/agents", "/knowledge", "/settings"]) {
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

test("IA y modelos: registrar una key, renombrar y eliminar un canal", async ({ page }, testInfo) => {
  // Escritorio y móvil comparten organización, que admite un WebChat y una configuración por proveedor: va en uno solo.
  test.skip(testInfo.project.name.includes("mobile"), "Usa recursos únicos por organización");
  const provider = "Claude";
  const providerName = `${provider} e2e ${Date.now()}`;
  const channelName = `Web e2e ${Date.now()}`;
  page.on("dialog", (dialog) => void dialog.accept());
  await login(page);

  // Registrar el proveedor (la key va cifrada al vault; no se valida contra el proveedor al guardar).
  await page.goto("/intelligence");
  await page.getByRole("button", { name: "Registrar proveedor" }).first().click();
  await page.getByRole("button", { name: provider, exact: true }).click();
  await page.getByLabel("Nombre visible").fill(providerName);
  await page.getByLabel("API key").fill("sk-e2e-no-real");
  await page.getByRole("button", { name: "Guardar en vault" }).click();
  await expect(page.getByText(providerName)).toBeVisible();

  // Canal web: se crea, se renombra y se elimina desde su tarjeta.
  await page.goto("/channels");
  await page.getByRole("button", { name: "Conectar canal" }).first().click();
  await page.getByRole("button", { name: "WebChat" }).click();
  await page.getByPlaceholder("WebChat Portal").fill(channelName);
  await page.getByRole("button", { name: "Guardar y conectar" }).click();
  await expect(page.getByRole("heading", { name: channelName })).toBeVisible();

  await page.getByRole("button", { name: `Editar ${channelName}` }).click();
  await page.getByLabel("Nombre", { exact: true }).fill(`${channelName} ventas`);
  await page.getByRole("button", { name: "Guardar" }).click();
  await expect(page.getByRole("heading", { name: `${channelName} ventas` })).toBeVisible();

  await page.getByRole("button", { name: `Eliminar ${channelName} ventas` }).click();
  await page.getByRole("button", { name: "Eliminar canal" }).click();
  await expect(page.getByRole("heading", { name: `${channelName} ventas` })).toHaveCount(0);

  // Eliminar el proveedor deja la organización como estaba.
  await page.goto("/intelligence");
  await page.getByRole("button", { name: `Eliminar ${providerName}` }).click();
  await expect(page.getByText(providerName)).toHaveCount(0);
});
