import { expect, test } from "@playwright/test";

const email = process.env.OCAP_ADMIN_EMAIL || "admin@ocap.io";
const password =
  process.env.OCAP_ADMIN_PASSWORD || "ChangeMe_Admin_2026!";

test("login, navegación principal y logout", async ({ page }) => {
  await page.goto("/login");

  await expect(page.getByRole("heading", { name: "Sign In" })).toBeVisible();
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign In" }).click();

  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByText(/Welcome back/i)).toBeVisible();

  for (const route of [
    "/channels",
    "/channels/webchat",
    "/intelligence",
    "/agents",
    "/workflows",
    "/workflows/designer",
    "/knowledge",
    "/monitoring",
    "/developer",
    "/security",
    "/settings",
  ]) {
    await page.goto(route);
    await expect(page).toHaveURL(new RegExp(`${route.replace(/\//g, "\\/")}$`));
    await expect(page.locator("main")).toBeVisible();
  }

  await page.getByTitle("Cerrar sesión").click();
  await expect(page).toHaveURL(/\/login$/);
});

test("instalador público muestra el asistente sin autenticación", async ({ page }) => {
  await page.goto("/installer");
  await expect(page.getByText(/Instalador OCAP/i).first()).toBeVisible();
  await expect(page.getByText(/Dónde vas a desplegar|Instalación marcada como completa/i).first()).toBeVisible();
  await expect(page.locator("main")).toBeVisible();
});

test("diseñador de workflows muestra toolbox usable", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign In" }).click();
  await expect(page).toHaveURL(/\/$/);

  await page.goto("/workflows/designer");
  await expect(page.getByText(/Diseñador de workflows/i)).toBeVisible();
  await expect(page.getByRole("button", { name: "Start" })).toBeVisible();
  await page.getByRole("button", { name: "Start" }).click();
  await expect(page.getByText(/^start$/i).first()).toBeVisible();
});