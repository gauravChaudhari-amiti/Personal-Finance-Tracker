import { useEffect, useMemo, useState } from "react";
import { accountService } from "../services/accountService";
import { budgetService } from "../services/budgetService";
import { categoryService } from "../services/categoryService";
import { goalService } from "../services/goalService";
import { transactionService } from "../services/transactionService";
import type { Account } from "../types/account";
import type { Budget } from "../types/budget";
import type { Category } from "../types/category";
import type { Goal } from "../types/goal";
import type { Transaction, TransactionListResponse } from "../types/transaction";
import { formatAccountDisplayName } from "../utils/accountDisplay";
import { hasVisibleCategoryName, sanitizeCategoryName } from "../utils/categoryName";

const wholeMonthCategoryName = "Whole Month";

const transferTypeLabels: Record<string, string> = {
  "transfer-in": "Transfer In",
  "transfer-out": "Transfer Out",
  "self-transfer-in": "Self Transfer In",
  "self-transfer-out": "Self Transfer Out",
  "card-settlement-in": "Card Payment",
  "card-settlement-out": "Card Payment"
};

const isOutgoingType = (transactionType: string) =>
  transactionType === "expense" ||
  transactionType === "transfer-out" ||
  transactionType === "self-transfer-out" ||
  transactionType === "card-settlement-out";

const isSystemTransferType = (transactionType: string) =>
  transactionType === "transfer-in" ||
  transactionType === "transfer-out" ||
  transactionType === "self-transfer-in" ||
  transactionType === "self-transfer-out" ||
  transactionType === "card-settlement-in" ||
  transactionType === "card-settlement-out";

const getTransactionTypeLabel = (transaction: Transaction) => {
  if (transaction.type === "card-settlement-in" || transaction.type === "card-settlement-out") {
    if (transaction.category === "Card Pay Off") {
      return "Pay Off";
    }

    if (transaction.category === "Card Pay Down") {
      return "Pay Down";
    }
  }

  return transferTypeLabels[transaction.type] || transaction.type;
};

type BudgetAlert = {
  tone: "warning" | "over" | "critical";
  message: string;
};

type SpendSourceOption = {
  value: string;
  label: string;
  helper?: string;
  kind: "account" | "goal";
};

export default function TransactionsPage() {
  const pageSize = 15;
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [goals, setGoals] = useState<Goal[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [budgetAlert, setBudgetAlert] = useState<BudgetAlert | null>(null);
  const [page, setPage] = useState(1);
  const [transactionMeta, setTransactionMeta] = useState<TransactionListResponse>({
    items: [],
    page: 1,
    pageSize,
    totalCount: 0,
    totalPages: 1,
    totalIncome: 0,
    totalExpense: 0
  });

  const [editingId, setEditingId] = useState<string | null>(null);

  const [accountId, setAccountId] = useState("");
  const [type, setType] = useState("expense");
  const [categoryId, setCategoryId] = useState("");
  const [amount, setAmount] = useState("");
  const [date, setDate] = useState("2026-03-16");
  const [merchant, setMerchant] = useState("");
  const [note, setNote] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("");
  const [tags, setTags] = useState("");

  const [filterType, setFilterType] = useState("");
  const [filterAccountId, setFilterAccountId] = useState("");
  const [search, setSearch] = useState("");

  const totalIncome = transactionMeta.totalIncome;
  const totalExpense = transactionMeta.totalExpense;

  const accountLookup = useMemo(
    () =>
      accounts.reduce<Record<string, Account>>((acc, account) => {
        acc[account.id] = account;
        return acc;
      }, {}),
    [accounts]
  );

  const goalLookup = useMemo(
    () =>
      goals.reduce<Record<string, Goal>>((acc, goal) => {
        acc[goal.id] = goal;
        return acc;
      }, {}),
    [goals]
  );

  const availableCategories = useMemo(
    () =>
      categories.filter(
        (category) =>
          category.type === type &&
          !category.isArchived &&
          category.name !== wholeMonthCategoryName &&
          hasVisibleCategoryName(category.name)
      ),
    [categories, type]
  );

  const matchingAccountFunds = useMemo(
    () =>
      type === "expense" && categoryId
        ? accounts.filter(
            (account) =>
              account.type === "Fund" &&
              account.categoryId === categoryId &&
              account.currentBalance > 0
          )
        : [],
    [accounts, type, categoryId]
  );

  const matchingGoalFunds = useMemo(
    () =>
      type === "expense" && categoryId
        ? goals.filter(
            (goal) =>
              goal.categoryId === categoryId &&
              goal.currentAmount > 0
          )
        : [],
    [goals, type, categoryId]
  );

  const normalSpendAccounts = useMemo(
    () => accounts.filter((account) => account.type !== "Fund"),
    [accounts]
  );

  const selectableAccounts = useMemo<SpendSourceOption[]>(() => {
    if (type !== "expense" || !categoryId) {
      return normalSpendAccounts.map((account) => ({
        value: `account:${account.id}`,
        label: formatAccountDisplayName(account.name, account.type),
        kind: "account"
      }));
    }

    const goalOptions = matchingGoalFunds.map((goal) => ({
      value: `goal:${goal.id}`,
      label: `${goal.name} (${goal.categoryName || "General"} Fund)`,
      helper: `Rs ${goal.currentAmount.toLocaleString()} available`,
      kind: "goal" as const
    }));

    const accountFundOptions = matchingAccountFunds.map((account) => ({
      value: `account:${account.id}`,
      label: `${account.name} (${account.categoryName || "General"} Fund)`,
      helper: `Rs ${account.currentBalance.toLocaleString()} available`,
      kind: "account" as const
    }));

    const normalAccountOptions = normalSpendAccounts.map((account) => ({
      value: `account:${account.id}`,
      label: formatAccountDisplayName(account.name, account.type),
      kind: "account" as const
    }));

    return [...normalAccountOptions, ...goalOptions, ...accountFundOptions];
  }, [type, categoryId, matchingAccountFunds, matchingGoalFunds, normalSpendAccounts]);

  const selectedCategoryName =
    availableCategories.find((category) => category.id === categoryId)?.name || "";

  const canSubmitTransaction =
    Boolean(categoryId) &&
    Boolean(accountId) &&
    Number(amount) > 0 &&
    !isSubmitting;

  const loadAccounts = async () => {
    const [accountData, goalData] = await Promise.all([
      accountService.getAll(),
      goalService.getAll()
    ]);

    setAccounts(accountData);
    setGoals(goalData);

    if (accountData.length > 0 && !accountId) {
      setAccountId(`account:${accountData[0].id}`);
    }
  };

  const loadTransactions = async (pageNumber = page) => {
    const data = await transactionService.getAll({
      type: filterType || undefined,
      accountId: filterAccountId || undefined,
      search: search || undefined,
      page: pageNumber,
      pageSize
    });

    if (data.items.length === 0 && data.totalCount > 0 && pageNumber > data.totalPages) {
      setPage(data.totalPages);
      return;
    }

    setTransactions(data.items);
    setTransactionMeta(data);
  };

  const loadCategories = async () => {
    const data = await categoryService.getAll();
    setCategories(data);
  };

  const initialLoad = async () => {
    try {
      setError("");
      await loadAccounts();
      await loadCategories();
      await loadTransactions();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to load transactions.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    initialLoad();
  }, []);

  useEffect(() => {
    if (!loading) {
      loadTransactions().catch((err: any) => {
        setError(err?.response?.data?.message || "Failed to load transactions.");
      });
    }
  }, [page]);

  useEffect(() => {
    if (categoryId && !availableCategories.some((category) => category.id === categoryId)) {
      setCategoryId("");
    }
  }, [type, categoryId, availableCategories]);

  useEffect(() => {
    if (selectableAccounts.length === 0) {
      if (accountId) {
        setAccountId("");
      }

      return;
    }

    if (!selectableAccounts.some((source) => source.value === accountId)) {
      setAccountId(selectableAccounts[0].value);
    }
  }, [selectableAccounts, accountId]);

  const resetForm = () => {
    setEditingId(null);
    setType("expense");
    setCategoryId("");
    setAmount("");
    setDate("2026-03-16");
    setMerchant("");
    setNote("");
    setPaymentMethod("");
    setTags("");
  };

  const buildBudgetAlert = (budget: Budget): BudgetAlert | null => {
    const progressLabel = budget.progressPercent.toFixed(0);
    const amountLabel = budget.amount.toLocaleString();
    const remainingAmount = Math.abs(budget.remainingAmount).toLocaleString();
    const budgetName =
      budget.categoryName === wholeMonthCategoryName ? "Whole Month budget" : `${budget.categoryName} budget`;

    if (budget.status === "critical") {
      return {
        tone: "critical",
        message: `${budgetName} has reached ${progressLabel}% of its Rs ${amountLabel} limit. You are Rs ${remainingAmount} over budget.`
      };
    }

    if (budget.status === "over") {
      return {
        tone: "over",
        message: `${budgetName} has gone over budget. You are Rs ${remainingAmount} above the Rs ${amountLabel} limit.`
      };
    }

    if (budget.status === "warning") {
      return {
        tone: "warning",
        message: `${budgetName} has reached ${progressLabel}% and crossed the ${budget.alertThresholdPercent}% alert threshold.`
      };
    }

    return null;
  };

  const refreshBudgetAlert = async (
    transactionType: string,
    selectedCategoryId: string,
    transactionDate: string
  ) => {
    if (transactionType !== "expense" || !selectedCategoryId || !transactionDate) {
      setBudgetAlert(null);
      return;
    }

    const [yearValue, monthValue] = transactionDate.split("-").map(Number);
    if (!monthValue || !yearValue) {
      setBudgetAlert(null);
      return;
    }

    try {
      const budgets = await budgetService.getAll(monthValue, yearValue);
      const matchingBudgets = budgets.filter(
        (budget) =>
          budget.categoryId === selectedCategoryId || budget.categoryName === wholeMonthCategoryName
      );

      const activeAlerts = matchingBudgets
        .map(buildBudgetAlert)
        .filter((alert): alert is BudgetAlert => alert !== null);

      if (activeAlerts.length === 0) {
        setBudgetAlert(null);
        return;
      }

      const tonePriority: Record<BudgetAlert["tone"], number> = {
        warning: 1,
        over: 2,
        critical: 3
      };

      const strongestAlert = activeAlerts.reduce((current, candidate) =>
        tonePriority[candidate.tone] > tonePriority[current.tone] ? candidate : current
      );

      setBudgetAlert({
        tone: strongestAlert.tone,
        message: activeAlerts.map((alert) => alert.message).join(" ")
      });
    } catch {
      setBudgetAlert(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setBudgetAlert(null);

    if (!categoryId) {
      setError("Category is required.");
      return;
    }

    if (!accountId) {
      setError("Select an account or a goal fund.");
      return;
    }

    if (Number(amount) <= 0) {
      setError("Amount must be greater than 0.");
      return;
    }

    setIsSubmitting(true);

    try {
      const savedType = type;
      const savedCategoryId = categoryId;
      const savedDate = date;
      const isGoalSource = accountId.startsWith("goal:");
      const resolvedSourceId = accountId.replace("goal:", "").replace("account:", "");

      const payload = {
        accountId: isGoalSource ? undefined : resolvedSourceId,
        goalId: isGoalSource ? resolvedSourceId : undefined,
        categoryId,
        type,
        amount: Number(amount),
        date,
        merchant,
        note,
        paymentMethod,
        tags: tags
          .split(",")
          .map((x) => x.trim())
          .filter(Boolean)
      };

      if (editingId) {
        await transactionService.update(editingId, payload);
      } else {
        await transactionService.create(payload);
      }

      await loadAccounts();
      await loadTransactions();
      await refreshBudgetAlert(savedType, savedCategoryId, savedDate);
      resetForm();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to save transaction.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleEdit = (transaction: Transaction) => {
    if (isSystemTransferType(transaction.type)) {
      setError("Transfer entries are system-generated and cannot be edited.");
      return;
    }

    setBudgetAlert(null);
    setEditingId(transaction.id);
    setAccountId(transaction.goalId ? `goal:${transaction.goalId}` : `account:${transaction.accountId}`);
    setType(transaction.type);
    setCategoryId(transaction.categoryId || "");
    setAmount(transaction.amount.toString());
    setDate(transaction.date.slice(0, 10));
    setMerchant(transaction.merchant || "");
    setNote(transaction.note || "");
    setPaymentMethod(transaction.paymentMethod || "");
    setTags(transaction.tags.join(", "));
  };

  const handleDelete = async (transaction: Transaction) => {
    if (isSystemTransferType(transaction.type)) {
      setError("Transfer entries are system-generated and cannot be deleted individually.");
      return;
    }

    const confirmed = window.confirm("Delete this transaction?");
    if (!confirmed) return;

    try {
      setBudgetAlert(null);
      await transactionService.delete(transaction.id);
      await loadAccounts();
      await loadTransactions();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to delete transaction.");
    }
  };

  const handleApplyFilters = async () => {
    try {
      setError("");
      if (page !== 1) {
        setPage(1);
        return;
      }

      await loadTransactions(1);
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to apply filters.");
    }
  };

  const handlePageChange = async (nextPage: number) => {
    if (nextPage < 1 || nextPage > transactionMeta.totalPages || nextPage === page) {
      return;
    }

    setPage(nextPage);
  };

  if (loading) return <div>Loading transactions...</div>;

  return (
    <div>
      <h1 className="page-title">Transactions</h1>

      {error && <div className="error-banner">{error}</div>}
      {budgetAlert && (
        <div
          className={`notice-banner ${budgetAlert.tone}`}
          role="status"
          aria-live="polite"
          data-testid="budget-alert"
        >
          {budgetAlert.message}
        </div>
      )}

      <div className="cards-grid">
        <div className="card">
          <h3>Total Income</h3>
          <div className="big-number">Rs {totalIncome.toLocaleString()}</div>
        </div>
        <div className="card">
          <h3>Total Expense</h3>
          <div className="big-number">Rs {totalExpense.toLocaleString()}</div>
        </div>
        <div className="card">
          <h3>Total Records</h3>
          <div className="big-number">{transactionMeta.totalCount}</div>
        </div>
      </div>

      <div className="section-grid-uneven">
        <div className="card">
          <h3>{editingId ? "Edit Transaction" : "Add Transaction"}</h3>

          <form className="form-grid" onSubmit={handleSubmit} data-testid="transaction-form">
            <div className="form-group">
              <label>Type</label>
              <select value={type} onChange={(e) => setType(e.target.value)} disabled={isSubmitting}>
                <option value="expense">Expense</option>
                <option value="income">Income</option>
              </select>
            </div>

            <div className="form-group">
              <label>Amount</label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                disabled={isSubmitting}
              />
            </div>

            <div className="form-group">
              <label>Date</label>
              <input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                disabled={isSubmitting}
              />
            </div>

            <div className="form-group">
              <label>Category</label>
              <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} disabled={isSubmitting}>
                <option value="">Select category</option>
                {availableCategories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {sanitizeCategoryName(category.name)}
                  </option>
                ))}
              </select>
              {!categoryId && <div className="helper-text">Choose a category before saving.</div>}
            </div>

            <div className="form-group">
              <label>Account / Fund</label>
              <select
                value={accountId}
                onChange={(e) => setAccountId(e.target.value)}
                disabled={isSubmitting}
                data-testid="transaction-account-fund"
              >
                <option value="">Select account or fund</option>
                <optgroup label="Accounts">
                  {normalSpendAccounts.map((account) => (
                    <option key={`account:${account.id}`} value={`account:${account.id}`}>
                      {formatAccountDisplayName(account.name, account.type)}
                    </option>
                  ))}
                </optgroup>
                {type === "expense" && categoryId && (matchingGoalFunds.length > 0 || matchingAccountFunds.length > 0) && (
                  <optgroup label={`${selectedCategoryName} Funds`}>
                    {matchingGoalFunds.map((goal) => (
                      <option key={`goal:${goal.id}`} value={`goal:${goal.id}`}>
                        {goal.name} ({goal.categoryName || "General"} Fund) - Rs {goal.currentAmount.toLocaleString()} available
                      </option>
                    ))}
                    {matchingAccountFunds.map((account) => (
                      <option key={`account:${account.id}`} value={`account:${account.id}`}>
                        {account.name} ({account.categoryName || "General"} Fund) - Rs {account.currentBalance.toLocaleString()} available
                      </option>
                    ))}
                  </optgroup>
                )}
              </select>
              {type === "expense" && categoryId && (matchingGoalFunds.length > 0 || matchingAccountFunds.length > 0) && (
                <div className="helper-text">
                  Only funds linked to {selectedCategoryName} are shown in the fund section.{" "}
                  {selectedCategoryName} funds available:{" "}
                  {[...matchingGoalFunds.map((goal) => `${goal.name} (Goal, Rs ${goal.currentAmount.toLocaleString()})`),
                    ...matchingAccountFunds.map((fund) => `${fund.name} (Account, Rs ${fund.currentBalance.toLocaleString()})`)]
                    .join(", ")}
                </div>
              )}
            </div>

            <div className="form-group">
              <label>Merchant</label>
              <input value={merchant} onChange={(e) => setMerchant(e.target.value)} disabled={isSubmitting} />
            </div>

            <div className="form-group">
              <label>Payment Method</label>
              <input
                value={paymentMethod}
                onChange={(e) => setPaymentMethod(e.target.value)}
                disabled={isSubmitting}
              />
            </div>

            <div className="form-group">
              <label>Tags (comma separated)</label>
              <input value={tags} onChange={(e) => setTags(e.target.value)} disabled={isSubmitting} />
            </div>

            <div className="form-group form-group-full">
              <label>Note</label>
              <textarea value={note} onChange={(e) => setNote(e.target.value)} rows={3} disabled={isSubmitting} />
            </div>

            <div className="form-actions">
              <button className="primary-btn" type="submit" disabled={!canSubmitTransaction}>
                {isSubmitting ? "Saving..." : editingId ? "Update Transaction" : "Save Transaction"}
              </button>

              {editingId && (
                <button
                  type="button"
                  className="secondary-btn-inline"
                  onClick={resetForm}
                  disabled={isSubmitting}
                >
                  Cancel Edit
                </button>
              )}
            </div>
          </form>
        </div>

        <div className="card">
          <h3>Filters</h3>

          <div className="form-grid">
            <div className="form-group">
              <label>Type</label>
              <select value={filterType} onChange={(e) => setFilterType(e.target.value)}>
                <option value="">All</option>
                <option value="income">Income</option>
                <option value="expense">Expense</option>
                <option value="transfer-in">Transfer In</option>
                <option value="transfer-out">Transfer Out</option>
                <option value="self-transfer-in">Self Transfer In</option>
                <option value="self-transfer-out">Self Transfer Out</option>
                <option value="card-settlement-in">Settlement In</option>
                <option value="card-settlement-out">Settlement Out</option>
              </select>
            </div>

            <div className="form-group">
              <label>Account</label>
              <select value={filterAccountId} onChange={(e) => setFilterAccountId(e.target.value)}>
                <option value="">All Accounts</option>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {formatAccountDisplayName(account.name, account.type)}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group form-group-full">
              <label>Search</label>
              <input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search by merchant or note"
              />
            </div>

            <button className="primary-btn" type="button" onClick={handleApplyFilters}>
              Apply Filters
            </button>
          </div>
        </div>
      </div>

      <div className="card">
        <h3>All Transactions</h3>

        <div className="table-wrapper">
          <table className="app-table">
            <thead>
              <tr>
                <th>Txn #</th>
                <th>Date</th>
                <th>Merchant</th>
                <th>Category</th>
                <th>Account</th>
                <th>Type</th>
                <th className="transaction-amount-cell">Amount</th>
                <th className="transaction-note-cell">Note</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((transaction) => (
                <tr key={transaction.id}>
                  <td>#{transaction.transactionNumber}</td>
                  <td>{transaction.date.slice(0, 10)}</td>
                  <td>{transaction.merchant || "-"}</td>
                  <td>{transaction.category || "-"}</td>
                  <td>
                    {transaction.goalId
                      ? `${goalLookup[transaction.goalId]?.name || transaction.goalName || transaction.accountName} (Goal Fund)`
                      : formatAccountDisplayName(
                          accountLookup[transaction.accountId || ""]?.name || transaction.accountName,
                          accountLookup[transaction.accountId || ""]?.type
                        )}
                  </td>
                  <td>
                    <span className={`status-pill ${transaction.type}`}>
                      {getTransactionTypeLabel(transaction)}
                    </span>
                  </td>
                  <td className="transaction-amount-cell">
                    {isOutgoingType(transaction.type) ? "-" : "+"}
                    Rs {transaction.amount.toLocaleString()}
                  </td>
                  <td className="transaction-note-cell">{transaction.note || "-"}</td>
                  <td>
                    <div className="row-actions">
                      <button className="link-btn" onClick={() => handleEdit(transaction)}>
                        Edit
                      </button>
                      <button className="link-btn danger" onClick={() => handleDelete(transaction)}>
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}

              {transactions.length === 0 && (
                <tr>
                  <td colSpan={9}>No transactions found.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {transactionMeta.totalPages > 1 && (
          <div className="pagination-row">
            {Array.from({ length: transactionMeta.totalPages }, (_, index) => index + 1).map((pageNumber) => (
              <button
                key={pageNumber}
                type="button"
                className={`pagination-btn ${pageNumber === page ? "active" : ""}`}
                onClick={() => handlePageChange(pageNumber)}
              >
                {pageNumber}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
