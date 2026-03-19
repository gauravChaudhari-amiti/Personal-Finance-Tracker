import { expect, test } from "@playwright/test";
import { formField, registerAndLogin, uniqueSuffix } from "./helpers";

test("registers, persists theme, and expires stale session on visibility check", async ({ page }) => {
  const loginCard = page.locator(".auth-card");

  await page.goto("/login");
  await expect(page.getByRole("heading", { name: "Personal Finance Tracker" })).toBeVisible();

  const initialTheme = await page.evaluate(() => document.documentElement.getAttribute("data-theme"));
  await page.getByRole("button", { name: /Mode$/ }).click();
  const toggledTheme = await page.evaluate(() => document.documentElement.getAttribute("data-theme"));

  expect(toggledTheme).not.toBe(initialTheme);

  await page.reload();
  await expect
    .poll(async () => page.evaluate(() => document.documentElement.getAttribute("data-theme")))
    .toBe(toggledTheme);

  const creds = await registerAndLogin(page, uniqueSuffix());
  await expect(page.getByText(`Welcome back, ${creds.displayName}`)).toBeVisible();

  await page.evaluate(() => {
    localStorage.setItem("pft_last_activity", (Date.now() - (61 * 60 * 1000)).toString());
    document.dispatchEvent(new Event("visibilitychange"));
  });

  await expect(page).toHaveURL(/\/login$/);

  await formField(loginCard, "Email").fill(creds.email);
  await formField(loginCard, "Password").fill(creds.password);
  await loginCard.locator("form").getByRole("button", { name: "Log In" }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
});
