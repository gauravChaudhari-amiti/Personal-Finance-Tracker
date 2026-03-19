import { expect, test } from "@playwright/test";
import fs from "node:fs/promises";
import {
  cardByHeading,
  createExpenseCategory,
  formField,
  registerAndLogin,
  uniqueSuffix
} from "./helpers";

test("covers goals, recurring bills, dashboard urgency, and reports export", async ({ page }) => {
  const suffix = uniqueSuffix();
  const travelCategory = `QA Goal Travel ${suffix}`;
  const bankName = `QA Goal Bank ${suffix}`;
  const goalName = `QA Travel Goal ${suffix}`;
  const recurringTitle = `QA Rent ${suffix}`;

  await registerAndLogin(page, suffix);
  await createExpenseCategory(page, travelCategory);

  await page.getByRole("link", { name: /Accounts/ }).click();
  const accountForm = cardByHeading(page, "Add Account");
  await formField(accountForm, "Account Name").fill(bankName);
  await formField(accountForm, "Type").selectOption({ label: "Savings Account" });
  await formField(accountForm, "Opening Balance").fill("15000");
  await formField(accountForm, "Institution").fill("QA Goal Bank");
  await accountForm.getByRole("button", { name: "Create Account" }).click();

  await page.getByRole("link", { name: /Goals/ }).click();
  await expect(page.getByRole("heading", { name: "Goals", exact: true })).toBeVisible();

  const goalForm = cardByHeading(page, "Create Goal");
  await formField(goalForm, "Name").fill(goalName);
  await formField(goalForm, "Target Amount").fill("5000");
  await formField(goalForm, "Category").selectOption({ label: travelCategory });
  await formField(goalForm, "Linked Account").selectOption({ label: `${bankName} (Savings Account)` });
  await goalForm.getByRole("button", { name: "Create Goal" }).click();

  await expect(page.locator(".goal-list")).toContainText(goalName);

  const actionsCard = cardByHeading(page, "Goal Actions");
  await formField(actionsCard, "Goal").selectOption({ label: `${goalName} (${travelCategory})` });
  await formField(actionsCard, "Amount").fill("2500");
  await formField(actionsCard, "Note").fill("Initial contribution");
  await actionsCard.getByRole("button", { name: "Add Contribution" }).click();
  await expect(page.locator(".notice-banner.success")).toContainText("Contribution added successfully.");
  await expect(page.locator(".goal-list")).toContainText("Rs 2,500 saved");

  await actionsCard.getByRole("button", { name: "Withdraw" }).click();
  await formField(actionsCard, "Amount").fill("500");
  await formField(actionsCard, "Note").fill("Partial withdrawal");
  await actionsCard.getByRole("button", { name: "Withdraw Amount" }).click();
  await expect(page.locator(".notice-banner.success")).toContainText("Withdrawal recorded successfully.");
  await expect(page.locator(".goal-list")).toContainText("Rs 2,000 saved");

  await page.getByRole("link", { name: /Recurring/ }).click();
  await expect(page.getByRole("heading", { name: "Recurring", exact: true })).toBeVisible();

  const recurringForm = cardByHeading(page, "New Recurring Item");
  const dueDate = new Date();
  dueDate.setDate(dueDate.getDate() + 5);
  const dueDateIso = dueDate.toISOString().slice(0, 10);

  await formField(recurringForm, "Title").fill(recurringTitle);
  await formField(recurringForm, "Amount").fill("1800");
  await formField(recurringForm, "Category").selectOption({ label: travelCategory });
  await formField(recurringForm, "Account").selectOption({ label: `${bankName} (Savings Account)` });
  await formField(recurringForm, "Start Date").fill(dueDateIso);
  await formField(recurringForm, "Next Due Date").fill(dueDateIso);
  await recurringForm.getByRole("button", { name: "Save Recurring Item" }).click();

  await expect(page.locator(".recurring-list")).toContainText(recurringTitle);

  await page.getByRole("link", { name: /Dashboard/ }).click();
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();

  const billRow = page.locator(".bill-list-item", { hasText: recurringTitle }).first();
  await expect(billRow).toBeVisible();
  await expect(billRow).toHaveClass(/bill-warning/);
  await expect(billRow).toContainText("Due in 5 days");

  await page.getByRole("link", { name: /Reports/ }).click();
  await expect(page.getByRole("heading", { name: "Reports", exact: true })).toBeVisible();

  const reportFilters = cardByHeading(page, "Filters");
  await formField(reportFilters, "Category").selectOption({ label: travelCategory });
  await reportFilters.getByRole("button", { name: "Apply Filters" }).click();

  const downloadPromise = page.waitForEvent("download");
  await reportFilters.getByRole("button", { name: "Export CSV" }).click();
  const download = await downloadPromise;
  expect(download.suggestedFilename()).toContain("finance-report-");

  const tempPath = await download.path();
  expect(tempPath).toBeTruthy();
  const csvContent = await fs.readFile(tempPath!, "utf8");
  expect(csvContent).toContain("Date,Account,Type,Category,Merchant,Amount,PaymentMethod,Note,Tags");
});
