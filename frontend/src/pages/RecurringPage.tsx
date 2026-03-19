import { useEffect, useMemo, useState } from "react";
import { accountService } from "../services/accountService";
import { categoryService } from "../services/categoryService";
import { recurringService } from "../services/recurringService";
import type { Account } from "../types/account";
import type { Category } from "../types/category";
import type { RecurringTransaction } from "../types/recurring";
import { formatAccountDisplayName } from "../utils/accountDisplay";
import { hasVisibleCategoryName, sanitizeCategoryName } from "../utils/categoryName";

const frequencies = ["daily", "weekly", "monthly", "yearly"] as const;
const wholeMonthCategoryName = "Whole Month";

const formatLabel = (value: string) =>
  value
    .split("-")
    .map((part) => (part ? part.charAt(0).toUpperCase() + part.slice(1) : part))
    .join(" ");

export default function RecurringPage() {
  const [items, setItems] = useState<RecurringTransaction[]>([]);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [editingId, setEditingId] = useState<string | null>(null);
  const [title, setTitle] = useState("");
  const [type, setType] = useState<"income" | "expense">("expense");
  const [amount, setAmount] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [accountId, setAccountId] = useState("");
  const [frequency, setFrequency] = useState<(typeof frequencies)[number]>("monthly");
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [endDate, setEndDate] = useState("");
  const [nextRunDate, setNextRunDate] = useState(new Date().toISOString().slice(0, 10));
  const [autoCreateTransaction, setAutoCreateTransaction] = useState(true);
  const [isPaused, setIsPaused] = useState(false);

  const filteredCategories = useMemo(
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

  const upcomingCount = useMemo(
    () => items.filter((item) => !item.isPaused && item.type === "expense").length,
    [items]
  );

  const autoCreateCount = useMemo(
    () => items.filter((item) => item.autoCreateTransaction && !item.isPaused).length,
    [items]
  );

  const loadData = async () => {
    const [recurringData, accountsData, categoriesData] = await Promise.all([
      recurringService.getAll(),
      accountService.getAll(),
      categoryService.getAll()
    ]);

    setItems(recurringData);
    setAccounts(accountsData);
    setCategories(categoriesData);

    if (accountsData.length > 0 && !accountId) {
      setAccountId(accountsData[0].id);
    }
  };

  useEffect(() => {
    const initialLoad = async () => {
      try {
        setError("");
        await loadData();
      } catch (err: any) {
        setError(err?.response?.data?.message || "Failed to load recurring items.");
      } finally {
        setLoading(false);
      }
    };

    initialLoad();
  }, []);

  useEffect(() => {
    if (filteredCategories.length > 0 && !filteredCategories.some((item) => item.id === categoryId)) {
      setCategoryId(filteredCategories[0].id);
    }
  }, [filteredCategories, categoryId]);

  const resetForm = () => {
    setEditingId(null);
    setTitle("");
    setType("expense");
    setAmount("");
    setCategoryId(filteredCategories[0]?.id || "");
    setFrequency("monthly");
    setStartDate(new Date().toISOString().slice(0, 10));
    setEndDate("");
    setNextRunDate(new Date().toISOString().slice(0, 10));
    setAutoCreateTransaction(true);
    setIsPaused(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    try {
      const payload = {
        title,
        type,
        amount: Number(amount),
        categoryId,
        accountId,
        frequency,
        startDate,
        endDate: endDate || undefined,
        nextRunDate: nextRunDate || undefined,
        autoCreateTransaction,
        isPaused
      };

      if (editingId) {
        await recurringService.update(editingId, payload);
      } else {
        await recurringService.create(payload);
      }

      resetForm();
      await loadData();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to save recurring item.");
    }
  };

  const handleEdit = (item: RecurringTransaction) => {
    setEditingId(item.id);
    setTitle(item.title);
    setType(item.type);
    setAmount(item.amount.toString());
    setCategoryId(item.categoryId);
    setAccountId(item.accountId);
    setFrequency(item.frequency);
    setStartDate(item.startDate.slice(0, 10));
    setEndDate(item.endDate?.slice(0, 10) || "");
    setNextRunDate(item.nextRunDate.slice(0, 10));
    setAutoCreateTransaction(item.autoCreateTransaction);
    setIsPaused(item.isPaused);
  };

  const handleDelete = async (id: string) => {
    const confirmed = window.confirm("Delete this recurring item?");
    if (!confirmed) return;

    try {
      setError("");
      await recurringService.delete(id);
      await loadData();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to delete recurring item.");
    }
  };

  const handleTogglePause = async (item: RecurringTransaction) => {
    try {
      setError("");
      await recurringService.update(item.id, {
        title: item.title,
        type: item.type,
        amount: item.amount,
        categoryId: item.categoryId,
        accountId: item.accountId,
        frequency: item.frequency,
        startDate: item.startDate.slice(0, 10),
        endDate: item.endDate?.slice(0, 10) || undefined,
        nextRunDate: item.nextRunDate.slice(0, 10),
        autoCreateTransaction: item.autoCreateTransaction,
        isPaused: !item.isPaused
      });
      await loadData();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to update recurring item.");
    }
  };

  if (loading) return <div>Loading recurring items...</div>;

  return (
    <div>
      <h1 className="page-title">Recurring</h1>

      {error && <div className="error-banner">{error}</div>}

      <div className="cards-grid">
        <div className="card">
          <h3>Total Items</h3>
          <div className="big-number">{items.length}</div>
        </div>
        <div className="card">
          <h3>Upcoming Bills</h3>
          <div className="big-number">{upcomingCount}</div>
        </div>
        <div className="card">
          <h3>Auto Create On</h3>
          <div className="big-number">{autoCreateCount}</div>
        </div>
      </div>

      <div className="section-grid-uneven">
        <div className="card">
          <div className="section-header">
            <h3>{editingId ? "Edit Recurring Item" : "New Recurring Item"}</h3>
          </div>

          <form className="form-grid" onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Title</label>
              <input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Netflix" />
            </div>

            <div className="form-group">
              <label>Type</label>
              <select value={type} onChange={(e) => setType(e.target.value as "income" | "expense")}>
                <option value="expense">Expense</option>
                <option value="income">Income</option>
              </select>
            </div>

            <div className="form-group">
              <label>Amount</label>
              <input type="number" min="0" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} />
            </div>

            <div className="form-group">
              <label>Frequency</label>
              <select value={frequency} onChange={(e) => setFrequency(e.target.value as (typeof frequencies)[number])}>
                {frequencies.map((item) => (
                  <option key={item} value={item}>
                    {formatLabel(item)}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Category</label>
              <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
                <option value="">Select category</option>
                {filteredCategories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {sanitizeCategoryName(category.name)}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Account</label>
              <select value={accountId} onChange={(e) => setAccountId(e.target.value)}>
                <option value="">Select account</option>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {formatAccountDisplayName(account.name, account.type)}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Start Date</label>
              <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
            </div>

            <div className="form-group">
              <label>Next Due Date</label>
              <input type="date" value={nextRunDate} onChange={(e) => setNextRunDate(e.target.value)} />
            </div>

            <div className="form-group">
              <label>End Date</label>
              <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
            </div>

            <div className="form-group form-group-full">
              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={autoCreateTransaction}
                  onChange={(e) => setAutoCreateTransaction(e.target.checked)}
                />
                <span>Auto-create transactions when due</span>
              </label>
              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={isPaused}
                  onChange={(e) => setIsPaused(e.target.checked)}
                />
                <span>Pause this recurring item</span>
              </label>
            </div>

            <div className="form-actions">
              <button className="primary-btn" type="submit">
                {editingId ? "Update Recurring Item" : "Save Recurring Item"}
              </button>
              {editingId && (
                <button type="button" className="secondary-btn-inline" onClick={resetForm}>
                  Cancel Edit
                </button>
              )}
            </div>
          </form>
        </div>

        <div className="card">
          <div className="section-header">
            <h3>Notes</h3>
          </div>
          <p className="meta-text">
            Use auto-create for salary, subscriptions, and repeat bills you want posted automatically.
          </p>
          <p className="meta-text">
            Keep auto-create off if you just want reminders in the upcoming bills widget without generating transactions.
          </p>
        </div>
      </div>

      <div className="card">
        <h3>All Recurring Items</h3>

        <div className="recurring-list">
          {items.map((item) => (
            <div key={item.id} className="recurring-item">
              <div className="budget-item-header">
                <div>
                  <strong>{item.title}</strong>
                  <div className="meta-text">
                    {formatLabel(item.frequency)} | {formatLabel(item.type)} | {item.categoryName}
                  </div>
                  <div className="meta-text">
                    {formatAccountDisplayName(item.accountName, accounts.find((account) => account.id === item.accountId)?.type)}
                  </div>
                </div>
                <div className={`status-pill ${item.isPaused ? "archived" : "active"}`}>
                  {item.isPaused ? "Paused" : "Active"}
                </div>
              </div>

              <div className="recurring-meta-row">
                <div className="meta-text">Next due: {item.nextRunDate.slice(0, 10)}</div>
                <div className="meta-text">Amount: Rs {item.amount.toLocaleString()}</div>
                <div className="meta-text">
                  {item.autoCreateTransaction ? "Auto-create enabled" : "Reminder only"}
                </div>
              </div>

              <div className="budget-item-footer">
                <div className="meta-text">
                  {item.endDate ? `Ends: ${item.endDate.slice(0, 10)}` : "No end date"}
                </div>
                <div className="row-actions">
                  <button className="link-btn" onClick={() => handleEdit(item)}>
                    Edit
                  </button>
                  <button className="link-btn" onClick={() => handleTogglePause(item)}>
                    {item.isPaused ? "Resume" : "Pause"}
                  </button>
                  <button className="link-btn danger" onClick={() => handleDelete(item.id)}>
                    Delete
                  </button>
                </div>
              </div>
            </div>
          ))}

          {items.length === 0 && (
            <div className="goal-empty-state">
              <strong>No recurring items yet.</strong>
              <div className="meta-text">Create subscriptions, rent, salary, or any repeating transaction here.</div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
