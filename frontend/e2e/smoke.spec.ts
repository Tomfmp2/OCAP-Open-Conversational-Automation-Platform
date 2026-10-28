import { expect, test, type Page } from "@playwright/test";

/**
 * Smoke del camino crítico contra un stack levantado con Docker Compose.
 * La instalación debe tener un admin: configuración inicial hecha en /setup o admin por entorno
 * (BOOTSTRAP_ADMIN_EMAIL / BOOTSTRAP_ADMIN_PASSWORD). Sus credenciales llegan por OCAP_ADMIN_EMAIL / OCAP_ADMIN_PASSWORD.
 */
const email = process.env.OCAP_ADMIN_EMAIL ?? "";
// Sitio de un cliente donde se incrusta el widget (lo sirve page.route).
const clientSite = "http://cliente.test";

// En la prueba el panel está en la red privada de Docker y Chromium bloquearía que un sitio público use el widget
// (Local Network Access). En producción el widget se sirve desde el dominio público del panel y esto no aplica.
test.use({ launchOptions: { args: ["--disable-features=LocalNetworkAccessChecks,BlockInsecurePrivateNetworkRequests"] } });
const password = process.env.OCAP_ADMIN_PASSWORD ?? "";

test.beforeAll(() => {
  if (!email || !password) {
    throw new Error("Define OCAP_ADMIN_EMAIL y OCAP_ADMIN_PASSWORD con la cuenta de administrador de la instalación.");
  }
});

/** En pantallas chicas el menú está en un panel que hay que abrir. */
async function openMenu(page: Page) {
  const button = page.getByRole("button", { name: "Abrir menú" });
  if (await button.isVisible()) await button.click();
}

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
  await expect(page.getByRole("heading", { name: /En qué te ayudo/i })).toBeVisible();

  for (const route of ["/inbox", "/channels", "/intelligence", "/agents", "/knowledge", "/team", "/settings", "/account"]) {
    await page.goto(route);
    await expect(page).toHaveURL(new RegExp(`${route.replace(/\//g, "\\/")}$`));
    await expect(page.locator("main")).toBeVisible();
  }

  await openMenu(page);
  await page.getByRole("button", { name: "Cerrar sesión" }).click();
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
  // Texto de la fila (proveedor y modelo por defecto): el estado vacío también nombra a Claude.
  const modelRow = "Claude, claude-sonnet-5-5";
  const channelName = `Web e2e ${Date.now()}`;
  page.on("dialog", (dialog) => void dialog.accept());
  await login(page);

  // Registrar el proveedor (la key va cifrada al vault; no se valida contra el proveedor al guardar).
  await page.goto("/intelligence");
  await page.getByRole("button", { name: "Agregar modelo" }).first().click();
  await page.getByLabel("Proveedor").selectOption(provider);
  await page.getByLabel("API key").fill("sk-e2e-no-real");
  await page.getByRole("button", { name: "Agregar", exact: true }).click();
  await expect(page.getByText(modelRow).first()).toBeVisible();

  // Canal web: se crea, se renombra y se elimina desde su tarjeta.
  await page.goto("/channels");
  await page.getByRole("button", { name: "Conectar canal" }).first().click();
  await page.getByText("WebChat", { exact: true }).click();
  await page.getByLabel("Nombre").fill(channelName);
  await page.getByRole("button", { name: "Conectar", exact: true }).click();
  // En un stack recién levantado la primera conexión puede tardar más que la espera por defecto.
  await expect(page.getByText(channelName, { exact: true })).toBeVisible({ timeout: 15_000 });

  await page.getByRole("button", { name: `Configurar ${channelName}` }).click();
  const settings = page.getByRole("dialog");
  await settings.getByLabel("Nombre", { exact: true }).fill(`${channelName} ventas`);
  await settings.getByRole("button", { name: "Guardar", exact: true }).click();
  await expect(settings.getByRole("heading", { name: `${channelName} ventas` })).toBeVisible();

  await settings.getByRole("button", { name: "Eliminar canal" }).click();
  await settings.getByRole("button", { name: "Eliminar canal" }).click();
  await expect(page.getByText(`${channelName} ventas`, { exact: true })).toHaveCount(0);

  // Eliminar el proveedor deja la organización como estaba.
  await page.goto("/intelligence");
  await expect(page.getByText(modelRow)).toBeVisible();
  await page.getByRole("button", { name: `Editar ${provider}` }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Eliminar" }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Eliminar" }).click();
  await expect(page.getByText(modelRow)).toHaveCount(0);
});

test("WebChat: configurar el widget en el panel, incrustarlo en el sitio de un cliente y conversar", async ({ page }, testInfo) => {
  // Una organización admite un solo WebChat: corre solo en escritorio y después del test de canales.
  test.skip(testInfo.project.name.includes("mobile"), "Usa recursos únicos por organización");
  const channelName = `Web widget ${Date.now()}`;
  await login(page);

  await page.goto("/channels");
  await page.getByRole("button", { name: "Conectar canal" }).first().click();
  await page.getByText("WebChat", { exact: true }).click();
  await page.getByLabel("Nombre").fill(channelName);
  await page.getByRole("button", { name: "Conectar", exact: true }).click();
  await expect(page.getByText(channelName, { exact: true })).toBeVisible({ timeout: 15_000 });

  // Desde el panel: título, dominio autorizado y el código para pegar.
  await page.getByRole("button", { name: `Configurar ${channelName}` }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("Sitios donde se puede usar").fill(clientSite);
  const snippet = (await dialog.locator("code").textContent())!;
  await dialog.getByRole("button", { name: "Guardar sitios" }).click();
  await expect(dialog.getByText("Guardado")).toBeVisible();
  await dialog.getByRole("button", { name: "Listo" }).click();
  // El panel lo genera con su propio origen; en la prueba el sitio cliente lo pide por la red interna.
  const embed = snippet.replace(/src="[^"]*\/ocap-webchat\.js"/, `src="${testInfo.project.use.baseURL}/ocap-webchat.js"`);

  const pageErrors: string[] = [];
  const site = await page.context().newPage();
  site.on("pageerror", (error) => pageErrors.push(error.message));
  await site.route(`${clientSite}/**`, (route) =>
    route.fulfill({ contentType: "text/html", body: `<!doctype html><title>Tienda</title><h1>Tienda</h1>${embed}` })
  );
  await site.goto(`${clientSite}/`);
  await site.getByRole("button", { name: "Abrir chat" }).click();
  await expect(site.getByRole("dialog", { name: channelName })).toBeVisible();
  await site.getByRole("textbox", { name: "Mensaje" }).fill("Hola, ¿hacen envíos?");
  await site.keyboard.press("Enter");
  const log = site.getByRole("log");
  await expect(log).toContainText("Hola, ¿hacen envíos?");
  await expect(log.locator(".msg.agent")).toHaveCount(1, { timeout: 20_000 });
  expect(await site.evaluate(() => localStorage.length + sessionStorage.length)).toBe(0);
  expect(pageErrors).toEqual([]);

  // La conversación del visitante llega a la bandeja del equipo.
  await page.goto("/inbox");
  await expect(page.getByText("Hola, ¿hacen envíos?").first()).toBeVisible({ timeout: 15_000 });

  // Deja la organización como estaba.
  page.on("dialog", (d) => void d.accept());
  await page.goto("/channels");
  await page.getByRole("button", { name: `Configurar ${channelName}` }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Eliminar canal" }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Eliminar canal" }).click();
  await expect(page.getByText(channelName, { exact: true })).toHaveCount(0);
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
  await openMenu(operator);
  await expect(operator.getByRole("link", { name: "Conversaciones" }).first()).toBeVisible();
  await expect(operator.getByRole("link", { name: "Canales" })).toHaveCount(0);
  await operator.goto("/channels");
  await expect(operator.getByRole("heading", { name: "Acceso denegado" })).toBeVisible();
  await guest.close();
});
