import { expect, type Locator, type Page } from "@playwright/test";

export const uniqueSuffix = () => Date.now().toString();

export const authCredentialsFor = (suffix: string) => ({
  displayName: `QA Browser ${suffix}`,
  email: `qa.browser.${suffix}@finance.local`,
  password: `QaBrowser@${suffix}`
});

export const cardByHeading = (page: Page, heading: string) =>
  page.locator(".card").filter({
    has: page.getByRole("heading", { name: heading, exact: true })
  }).first();

export const formField = (scope: Locator, label: string) =>
  scope.locator(".form-group", { hasText: label }).locator("input, select, textarea").first();

export const formGroup = (scope: Locator, label: string) =>
  scope.locator(".form-group", { hasText: label }).first();

export async function registerAndLogin(page: Page, suffix = uniqueSuffix()) {
  const credentials = authCredentialsFor(suffix);

  await page.goto("/login");
  await page.locator(".auth-switch").getByRole("button", { name: "Create Account" }).click();

  await formField(page.locator(".auth-card"), "Display Name").fill(credentials.displayName);
  await formField(page.locator(".auth-card"), "Email").fill(credentials.email);
  await formField(page.locator(".auth-card"), "Password").fill(credentials.password);
  await formField(page.locator(".auth-card"), "Confirm Password").fill(credentials.password);
  await page.locator(".auth-card form").getByRole("button", { name: "Create Account" }).click();

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();

  return credentials;
}

export async function createExpenseCategory(page: Page, name: string) {
  await page.getByRole("link", { name: /Categories/ }).click();
  await expect(page.getByRole("heading", { name: "Categories", exact: true })).toBeVisible();

  const categoryCard = cardByHeading(page, "Add Category");
  await formField(categoryCard, "Name").fill(name);
  await formField(categoryCard, "Icon").fill("plane");
  await categoryCard.getByRole("button", { name: "Create Category" }).click();

  await expect(page.locator("table")).toContainText(name);
}

export async function createIncomeCategory(page: Page, name: string) {
  await page.getByRole("link", { name: /Categories/ }).click();
  await expect(page.getByRole("heading", { name: "Categories", exact: true })).toBeVisible();

  const categoryCard = cardByHeading(page, "Add Category");
  await categoryCard.getByRole("button", { name: "Income" }).click();
  await formField(categoryCard, "Name").fill(name);
  await formField(categoryCard, "Icon").fill("coins");
  await categoryCard.getByRole("button", { name: "Create Category" }).click();

  await expect(page.locator("table")).toContainText(name);
}
