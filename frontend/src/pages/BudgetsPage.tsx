import { useEffect, useMemo, useState } from "react";
import { budgetService } from "../services/budgetService";
import { categoryService } from "../services/categoryService";
import type { Budget } from "../types/budget";
import type { Category } from "../types/category";
import { hasVisibleCategoryName, sanitizeCategoryName } from "../utils/categoryName";

const now = new Date();
const wholeMonthCategoryName = "Whole Month";

export default function BudgetsPage() {
  const [budgets, setBudgets] = useState<Budget[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [month, setMonth] = useState(now.getMonth() + 1);
  const [year, setYear] = useState(now.getFullYear());

  const [editingId, setEditingId] = useState<string | null>(null);
  const [categoryId, setCategoryId] = useState("");
  const [amount, setAmount] = useState("");
  const [alertThresholdPercent, setAlertThresholdPercent] = useState("80");

  const expenseCategories = useMemo(
    () =>
      categories.filter(
        (category) =>
          category.type === "expense" &&
          !category.isArchived &&
          hasVisibleCategoryName(category.name)
      ),
    [categories]
  );

  const overallBudget = useMemo(
    () => budgets.find((budget) => budget.categoryName === wholeMonthCategoryName) || null,
    [budgets]
  );

  const totals = useMemo(() => {
    if (overallBudget) {
      return {
        budgeted: overallBudget.amount,
        spent: overallBudget.spentAmount
      };
    }

    return budgets.reduce(
      (acc, budget) => {
        acc.budgeted += budget.amount;
        acc.spent += budget.spentAmount;
        return acc;
      },
      { budgeted: 0, spent: 0 }
    );
  }, [budgets]);

  const loadCategories = async () => {
    const data = await categoryService.getAll({ type: "expense" });
    setCategories(data);

    if (data.length > 0 && !categoryId) {
      const preferredCategory =
        data.find((category) => category.name === wholeMonthCategoryName) ?? data[0];
      setCategoryId(preferredCategory.id);
    }
  };

  const loadBudgets = async () => {
    const data = await budgetService.getAll(month, year);
    setBudgets(data);
  };

  const initialLoad = async () => {
    try {
      setError("");
      await loadCategories();
      await loadBudgets();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to load budgets.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    initialLoad();
  }, []);

  useEffect(() => {
    const reload = async () => {
      try {
        setError("");
        await loadBudgets();
      } catch (err: any) {
        setError(err?.response?.data?.message || "Failed to load budgets.");
      }
    };

    reload();
  }, [month, year]);

  const resetForm = () => {
    setEditingId(null);
    setAmount("");
    setAlertThresholdPercent("80");
    if (expenseCategories.length > 0) {
      const preferredCategory =
        expenseCategories.find((category) => category.name === wholeMonthCategoryName) ??
        expenseCategories[0];
      setCategoryId(preferredCategory.id);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    try {
      const payload = {
        categoryId,
        month,
        year,
        amount: Number(amount),
        alertThresholdPercent: Number(alertThresholdPercent)
      };

      if (editingId) {
        await budgetService.update(editingId, payload);
      } else {
        await budgetService.create(payload);
      }

      resetForm();
      await loadBudgets();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to save budget.");
    }
  };

  const handleEdit = (budget: Budget) => {
    setEditingId(budget.id);
    setCategoryId(budget.categoryId);
    setAmount(budget.amount.toString());
    setAlertThresholdPercent(budget.alertThresholdPercent.toString());
  };

  const handleDelete = async (id: string) => {
    const confirmed = window.confirm("Delete this budget?");
    if (!confirmed) return;

    try {
      setError("");
      await budgetService.delete(id);
      await loadBudgets();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to delete budget.");
    }
  };

  const handleDuplicate = async () => {
    const sourceDate = new Date(year, month - 2, 1);

    try {
      setError("");
      await budgetService.duplicate({
        sourceMonth: sourceDate.getMonth() + 1,
        sourceYear: sourceDate.getFullYear(),
        targetMonth: month,
        targetYear: year
      });
      await loadBudgets();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to duplicate budgets.");
    }
  };

  if (loading) return <div>Loading budgets...</div>;

  return (
    <div>
      <h1 className="page-title">Budgets</h1>

      {error && <div className="error-banner">{error}</div>}

      <div className="cards-grid">
        <div className="card">
          <h3>{overallBudget ? "Whole Month Budget" : "Total Budgeted"}</h3>
          <div className="big-number">Rs {totals.budgeted.toLocaleString()}</div>
        </div>
        <div className="card">
          <h3>{overallBudget ? "Whole Month Spent" : "Total Spent"}</h3>
          <div className="big-number">Rs {totals.spent.toLocaleString()}</div>
        </div>
        <div className="card">
          <h3>{overallBudget ? "Whole Month Remaining" : "Remaining"}</h3>
          <div className="big-number">Rs {(totals.budgeted - totals.spent).toLocaleString()}</div>
        </div>
      </div>

      <div className="section-grid-uneven">
        <div className="card">
          <div className="section-header">
            <h3>{editingId ? "Edit Budget" : "Set Budget"}</h3>
          </div>

          <form className="form-grid" onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Month</label>
              <select value={month} onChange={(e) => setMonth(Number(e.target.value))}>
                {Array.from({ length: 12 }, (_, index) => index + 1).map((item) => (
                  <option key={item} value={item}>
                    {new Date(2026, item - 1, 1).toLocaleString("en-US", { month: "long" })}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Year</label>
              <input
                type="number"
                value={year}
                onChange={(e) => setYear(Number(e.target.value))}
                min="2000"
                max="3000"
              />
            </div>

            <div className="form-group">
              <label>Category</label>
              <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
                <option value="">Select category</option>
                {expenseCategories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name === wholeMonthCategoryName
                      ? `${wholeMonthCategoryName} (overall)`
                      : sanitizeCategoryName(category.name)}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Budget Amount</label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
              />
            </div>

            <div className="form-group">
              <label>Alert Threshold %</label>
              <input
                type="number"
                min="1"
                max="500"
                value={alertThresholdPercent}
                onChange={(e) => setAlertThresholdPercent(e.target.value)}
              />
            </div>

            <div className="form-actions">
              <button className="primary-btn" type="submit">
                {editingId ? "Update Budget" : "Save Budget"}
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
            <h3>Actions</h3>
          </div>
          <p className="meta-text">
            Copy the previous month&apos;s budgets into the selected month to save setup time, including the whole-month budget if you use it.
          </p>
          <button className="primary-btn" type="button" onClick={handleDuplicate}>
            Duplicate Last Month
          </button>
        </div>
      </div>

      <div className="card">
        <h3>Budget Progress</h3>

        <div className="budget-list">
          {budgets.map((budget) => (
            <div className="budget-item" key={budget.id}>
              <div className="budget-item-header">
                <div className="budget-title-group">
                  <span
                    className="color-chip"
                    style={{ backgroundColor: budget.categoryColor || "#94A3B8" }}
                  />
                  <div>
                    <strong>{budget.categoryName}</strong>
                    <div className="meta-text">
                      Rs {budget.spentAmount.toLocaleString()} spent of Rs {budget.amount.toLocaleString()}
                    </div>
                  </div>
                </div>
                <div className={`status-pill budget-${budget.status}`}>
                  {budget.progressPercent.toFixed(0)}%
                </div>
              </div>

              <div className="budget-bar">
                <div
                  className={`budget-bar-fill budget-${budget.status}`}
                  style={{ width: `${Math.min(budget.progressPercent, 100)}%` }}
                />
              </div>

              <div className="budget-item-footer">
                <div className="meta-text">
                  Remaining: Rs {budget.remainingAmount.toLocaleString()}
                </div>
                <div className="row-actions">
                  <button className="link-btn" onClick={() => handleEdit(budget)}>
                    Edit
                  </button>
                  <button className="link-btn danger" onClick={() => handleDelete(budget.id)}>
                    Delete
                  </button>
                </div>
              </div>
            </div>
          ))}

          {budgets.length === 0 && <div className="meta-text">No budgets set for this month yet.</div>}
        </div>
      </div>
    </div>
  );
}
