import { expect, test } from "@playwright/test";
import {
  cardByHeading,
  createExpenseCategory,
  createIncomeCategory,
  formField,
  registerAndLogin,
  uniqueSuffix
} from "./helpers";

test("covers categories, accounts, budgets, transactions, and settlement flows", async ({ page }) => {
  const suffix = uniqueSuffix();
  const travelCategory = `QA Travel ${suffix}`;
  const bonusCategory = `QA Bonus ${suffix}`;
  const bankName = `QA Bank ${suffix}`;
  const fundName = `QA Travel Fund ${suffix}`;
  const cardName = `QA Card ${suffix}`;
  const tempAccountName = `QA Temp ${suffix}`;

  await registerAndLogin(page, suffix);

  await createExpenseCategory(page, travelCategory);
  await createIncomeCategory(page, bonusCategory);

  await page.getByRole("link", { name: /Accounts/ }).click();
  await expect(page.getByRole("heading", { name: "Accounts", exact: true })).toBeVisible();

  const accountForm = cardByHeading(page, "Add Account");

  await formField(accountForm, "Account Name").fill(bankName);
  await formField(accountForm, "Type").selectOption({ label: "Savings Account" });
  await formField(accountForm, "Opening Balance").fill("20000");
  await formField(accountForm, "Institution").fill("QA Bank");
  await accountForm.getByRole("button", { name: "Create Account" }).click();
  await expect(page.locator("table")).toContainText(bankName);

  await formField(accountForm, "Account Name").fill(fundName);
  await formField(accountForm, "Type").selectOption({ label: "Fund" });
  await formField(accountForm, "Fund Category").selectOption({ label: travelCategory });
  await formField(accountForm, "Fund Balance").fill("4000");
  await formField(accountForm, "Institution").fill("Travel Bucket");
  await accountForm.getByRole("button", { name: "Create Account" }).click();
  await expect(page.locator("table")).toContainText(fundName);

  await formField(accountForm, "Account Name").fill(cardName);
  await formField(accountForm, "Type").selectOption({ label: "Credit Card" });
  await expect(formField(accountForm, "Credit Limit")).toBeVisible();
  await formField(accountForm, "Credit Limit").fill("12000");
  await formField(accountForm, "Institution").fill("QA Cards");
  await accountForm.getByRole("button", { name: "Create Account" }).click();
  await expect(page.locator("table")).toContainText(cardName);

  await formField(accountForm, "Account Name").fill(tempAccountName);
  await formField(accountForm, "Type").selectOption({ label: "Cash Wallet" });
  await formField(accountForm, "Opening Balance").fill("250");
  await accountForm.getByRole("button", { name: "Create Account" }).click();
  await expect(page.locator("table")).toContainText(tempAccountName);

  page.once("dialog", (dialog) => dialog.accept());
  await page.locator("tr", { hasText: tempAccountName }).getByRole("button", { name: "Delete" }).click();
  await expect(page.locator("table")).not.toContainText(tempAccountName);

  await page.getByRole("link", { name: /Budgets/ }).click();
  await expect(page.getByRole("heading", { name: "Budgets", exact: true })).toBeVisible();

  const budgetForm = cardByHeading(page, "Set Budget");
  await formField(budgetForm, "Category").selectOption({ label: travelCategory });
  await formField(budgetForm, "Budget Amount").fill("5000");
  await formField(budgetForm, "Alert Threshold %").fill("50");
  await budgetForm.getByRole("button", { name: "Save Budget" }).click();
  await expect(page.locator(".budget-list")).toContainText(travelCategory);

  await page.getByRole("link", { name: /Transactions/ }).click();
  await expect(page.getByRole("heading", { name: "Transactions", exact: true })).toBeVisible();

  const transactionForm = cardByHeading(page, "Add Transaction");

  await formField(transactionForm, "Type").selectOption({ label: "Income" });
  await formField(transactionForm, "Amount").fill("5000");
  await formField(transactionForm, "Category").selectOption({ label: bonusCategory });
  await formField(transactionForm, "Account / Fund").selectOption({
    label: `${bankName} (Savings Account)`
  });
  await formField(transactionForm, "Merchant").fill("QA Employer");
  await formField(transactionForm, "Payment Method").fill("Bank Transfer");
  await formField(transactionForm, "Note").fill("Income for UI test");
  await transactionForm.getByRole("button", { name: "Save Transaction" }).click();

  await formField(transactionForm, "Type").selectOption({ label: "Expense" });
  await formField(transactionForm, "Category").selectOption({ label: travelCategory });
  await expect(formField(transactionForm, "Account / Fund")).toContainText(fundName);
  await formField(transactionForm, "Account / Fund").selectOption({
    label: `${fundName} (${travelCategory} Fund) - Rs 4,000 available`
  });
  await formField(transactionForm, "Amount").fill("2700");
  await expect(formField(transactionForm, "Amount")).toHaveValue("2700");
  await formField(transactionForm, "Merchant").fill("Travel Agency");
  await formField(transactionForm, "Payment Method").fill("Fund");
  await formField(transactionForm, "Note").fill("Travel fund expense");
  await transactionForm.getByRole("button", { name: "Save Transaction" }).click();

  await expect(page.locator("table")).toContainText("Travel Agency");

  await formField(transactionForm, "Category").selectOption({ label: travelCategory });
  await formField(transactionForm, "Account / Fund").selectOption({
    label: `${cardName} (Credit Card)`
  });
  await formField(transactionForm, "Amount").fill("3000");
  await expect(formField(transactionForm, "Amount")).toHaveValue("3000");
  await formField(transactionForm, "Merchant").fill("Airline");
  await formField(transactionForm, "Payment Method").fill("Card");
  await formField(transactionForm, "Note").fill("Card travel expense");
  await transactionForm.getByRole("button", { name: "Save Transaction" }).click();

  await page.locator("tr", { hasText: "Travel Agency" }).getByRole("button", { name: "Edit" }).click();
  const editTransactionForm = cardByHeading(page, "Edit Transaction");
  await formField(editTransactionForm, "Amount").fill("2500");
  await formField(editTransactionForm, "Note").fill("Travel fund expense updated");
  await editTransactionForm.getByRole("button", { name: "Update Transaction" }).click();
  await expect(page.locator("table")).toContainText("Travel fund expense updated");

  await formField(cardByHeading(page, "Filters"), "Search").fill("Travel fund expense updated");
  await cardByHeading(page, "Filters").getByRole("button", { name: "Apply Filters" }).click();
  await expect(page.locator("table")).toContainText("Travel fund expense updated");

  await page.getByRole("link", { name: /Accounts/ }).click();
  await expect(page.locator("table")).toContainText("Rs 1,500");
  await expect(page.locator("table")).toContainText("Rs 3,000 due");

  const transferCard = page
    .locator(".card", { hasText: "Source Account" })
    .filter({ has: page.locator(".form-group", { hasText: "Destination Account" }) })
    .first();
  await formField(transferCard, "Amount").fill("2000");
  await formField(transferCard, "Note").fill("Card settlement");
  await transferCard.getByRole("button", { name: "Pay Down" }).click();

  await expect(page.locator("table")).toContainText("Rs 1,000 due");
});
