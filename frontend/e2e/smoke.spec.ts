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
  // La CSP con nonce no debe bloquear nada propio del panel: cualquier violación es un error.
  const cspViolations: string[] = [];
  page.on("console", (message) => {
    if (/Content Security Policy|Content-Security-Policy/i.test(message.text())) cspViolations.push(message.text());
  });
  const response = await page.goto("/login");
  expect(response?.headers()["content-security-policy"]).toContain("'nonce-");
  expect(response?.headers()["x-frame-options"]).toBe("DENY");
  expect(response?.headers()["x-powered-by"]).toBeUndefined();

  await login(page);
  await expect(page.getByRole("heading", { name: /En qué puedo ayudarte/i })).toBeVisible();

  for (const route of ["/inbox", "/channels", "/channels/webchat", "/intelligence", "/agents", "/knowledge", "/settings"]) {
    await page.goto(route);
    await expect(page).toHaveURL(new RegExp(`${route.replace(/\//g, "\\/")}$`));
    await expect(page.locator("main")).toBeVisible();
  }

  await page.getByTitle("Cerrar sesión").click();
  await expect(page).toHaveURL(/\/login$/);
  expect(cspViolations).toEqual([]);
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
  // En un stack recién levantado la primera conexión puede tardar más que la espera por defecto.
  await expect(page.getByRole("heading", { name: channelName })).toBeVisible({ timeout: 15_000 });

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

test("equipo: invitar a un operador, aceptar el enlace y ver solo lo suyo", async ({ page, browser }, testInfo) => {
  // La invitación crea una cuenta: va en un solo proyecto para no repetir el correo.
  test.skip(testInfo.project.name.includes("mobile"), "Crea una cuenta única");
  const email = `operador-${Date.now()}@ocap.io`;
  const operatorPassword = "operadora-segura-2026";
  await login(page);

  await page.goto("/team");
  await page.getByRole("button", { name: "Invitar" }).first().click();
  await page.getByLabel("Nombre").fill("Operadora E2E");
  await page.getByLabel("Correo").fill(email);
  await page.getByLabel("Rol").selectOption("Operator");
  await page.getByRole("button", { name: "Invitar", exact: true }).last().click();
  const link = await page.getByLabel("Enlace para definir la contraseña").inputValue();
  expect(link).toContain("/accept?token=");

  // La persona invitada abre el enlace en otro navegador (sin la sesión del administrador).
  const guest = await browser.newContext({ baseURL: testInfo.project.use.baseURL });
  const operator = await guest.newPage();
  await operator.goto(new URL(link).pathname + new URL(link).search);
  await operator.getByLabel("Nueva contraseña").fill(operatorPassword);
  await operator.getByLabel("Repite la contraseña").fill(operatorPassword);
  await operator.getByRole("button", { name: "Guardar contraseña" }).click();
  await expect(operator.getByRole("heading", { name: "Contraseña guardada" })).toBeVisible();

  await operator.goto("/login");
  await operator.getByLabel("Email").fill(email);
  await operator.getByLabel("Contraseña").fill(operatorPassword);
  await operator.getByRole("button", { name: "Entrar" }).click();
  await expect(operator).toHaveURL(/\/$/);

  // Solo ve el asistente y la bandeja; Canales por URL dice que no tiene acceso.
  await expect(operator.getByRole("link", { name: "Conversaciones" }).first()).toBeVisible();
  await expect(operator.getByRole("link", { name: "Canales" })).toHaveCount(0);
  await operator.goto("/channels");
  await expect(operator.getByRole("heading", { name: "Acceso denegado" })).toBeVisible();
  await guest.close();
});
