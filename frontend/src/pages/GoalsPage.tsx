import { useEffect, useMemo, useState } from "react";
import { accountService } from "../services/accountService";
import { categoryService } from "../services/categoryService";
import { goalService } from "../services/goalService";
import type { Account } from "../types/account";
import type { Category } from "../types/category";
import type { Goal } from "../types/goal";
import { formatAccountDisplayName } from "../utils/accountDisplay";
import { hasVisibleCategoryName, sanitizeCategoryName } from "../utils/categoryName";

const palette = [
  "#2563EB",
  "#10B981",
  "#F97316",
  "#EAB308",
  "#EC4899",
  "#8B5CF6",
  "#14B8A6",
  "#EF4444"
];

type GoalActionMode = "contribute" | "withdraw";

export default function GoalsPage() {
  const [goals, setGoals] = useState<Goal[]>([]);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [targetAmount, setTargetAmount] = useState("");
  const [targetDate, setTargetDate] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [linkedAccountId, setLinkedAccountId] = useState("");
  const [icon, setIcon] = useState("");
  const [color, setColor] = useState(palette[0]);

  const [selectedGoalId, setSelectedGoalId] = useState("");
  const [actionMode, setActionMode] = useState<GoalActionMode>("contribute");
  const [actionAmount, setActionAmount] = useState("");
  const [actionAccountId, setActionAccountId] = useState("");
  const [actionNote, setActionNote] = useState("");

  const totals = useMemo(() => {
    return goals.reduce(
      (acc, goal) => {
        acc.saved += goal.currentAmount;
        acc.target += goal.targetAmount;
        if (goal.status === "completed") {
          acc.completed += 1;
        } else {
          acc.active += 1;
        }
        return acc;
      },
      { saved: 0, target: 0, active: 0, completed: 0 }
    );
  }, [goals]);

  const selectedGoal = useMemo(
    () => goals.find((goal) => goal.id === selectedGoalId) || null,
    [goals, selectedGoalId]
  );

  const accountLookup = useMemo(
    () =>
      accounts.reduce<Record<string, Account>>((acc, account) => {
        acc[account.id] = account;
        return acc;
      }, {}),
    [accounts]
  );

  const eligibleAccounts = useMemo(
    () => accounts.filter((account) => account.type !== "Credit Card"),
    [accounts]
  );

  const expenseCategories = useMemo(
    () =>
      categories.filter(
        (category) =>
          category.type === "expense" &&
          !category.isArchived &&
          category.name !== "Whole Month" &&
          hasVisibleCategoryName(category.name)
      ),
    [categories]
  );

  const loadData = async () => {
    const [goalsData, accountsData, categoriesData] = await Promise.all([
      goalService.getAll(),
      accountService.getAll(),
      categoryService.getAll({ type: "expense" })
    ]);

    setGoals(goalsData);
    setAccounts(accountsData);
    setCategories(categoriesData.filter((category) => category.name !== "Whole Month"));

    if (goalsData.length > 0 && !selectedGoalId) {
      setSelectedGoalId(goalsData[0].id);
    }
  };

  useEffect(() => {
    const initialLoad = async () => {
      try {
        setError("");
        await loadData();
      } catch (err: any) {
        setError(err?.response?.data?.message || "Failed to load goals.");
      } finally {
        setLoading(false);
      }
    };

    initialLoad();
  }, []);

  useEffect(() => {
    if (selectedGoal?.linkedAccountId) {
      setActionAccountId(selectedGoal.linkedAccountId);
      return;
    }

    setActionAccountId("");
  }, [selectedGoalId, selectedGoal?.linkedAccountId]);

  useEffect(() => {
    if (categoryId && !expenseCategories.some((category) => category.id === categoryId)) {
      setCategoryId("");
    }
  }, [categoryId, expenseCategories]);

  const resetGoalForm = () => {
    setEditingId(null);
    setName("");
    setTargetAmount("");
    setTargetDate("");
    setCategoryId("");
    setLinkedAccountId("");
    setIcon("");
    setColor(palette[0]);
  };

  const resetActionForm = () => {
    setActionAmount("");
    setActionNote("");
    if (selectedGoal?.linkedAccountId) {
      setActionAccountId(selectedGoal.linkedAccountId);
      return;
    }

    setActionAccountId("");
  };

  const handleGoalSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSuccessMessage("");

    try {
      const payload = {
        name,
        targetAmount: Number(targetAmount),
        targetDate: targetDate || undefined,
        categoryId: categoryId || undefined,
        linkedAccountId: linkedAccountId || undefined,
        icon: icon || undefined,
        color
      };

      if (editingId) {
        await goalService.update(editingId, payload);
        setSuccessMessage("Goal updated successfully.");
      } else {
        await goalService.create(payload);
        setSuccessMessage("Goal created successfully.");
      }

      resetGoalForm();
      await loadData();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to save goal.");
    }
  };

  const handleGoalEdit = (goal: Goal) => {
    setSuccessMessage("");
    setEditingId(goal.id);
    setName(goal.name);
    setTargetAmount(goal.targetAmount.toString());
    setTargetDate(goal.targetDate?.slice(0, 10) || "");
    setCategoryId(goal.categoryId || "");
    setLinkedAccountId(goal.linkedAccountId || "");
    setIcon(goal.icon || "");
    setColor(goal.color || palette[0]);
  };

  const handleGoalDelete = async (goal: Goal) => {
    const confirmed = window.confirm(`Delete the goal "${goal.name}"?`);
    if (!confirmed) return;

    try {
      setError("");
      setSuccessMessage("");
      await goalService.delete(goal.id);
      if (selectedGoalId === goal.id) {
        setSelectedGoalId("");
      }
      await loadData();
      setSuccessMessage("Goal deleted successfully.");
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to delete goal.");
    }
  };

  const handleGoalAction = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!selectedGoal) return;

    try {
      setError("");
      setSuccessMessage("");

      const payload = {
        amount: Number(actionAmount),
        accountId: actionAccountId || undefined,
        note: actionNote || undefined
      };

      if (actionMode === "contribute") {
        await goalService.contribute(selectedGoal.id, payload);
        setSuccessMessage("Contribution added successfully.");
      } else {
        await goalService.withdraw(selectedGoal.id, payload);
        setSuccessMessage("Withdrawal recorded successfully.");
      }

      resetActionForm();
      await loadData();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to update goal.");
    }
  };

  if (loading) return <div>Loading goals...</div>;

  return (
    <div>
      <h1 className="page-title">Goals</h1>

      {error && <div className="error-banner">{error}</div>}
      {successMessage && <div className="notice-banner success">{successMessage}</div>}

      <div className="cards-grid">
        <div className="card">
          <h3>Total Saved</h3>
          <div className="big-number">Rs {totals.saved.toLocaleString()}</div>
        </div>
        <div className="card">
          <h3>Active Goals</h3>
          <div className="big-number">{totals.active}</div>
        </div>
        <div className="card">
          <h3>Completed Goals</h3>
          <div className="big-number">{totals.completed}</div>
        </div>
      </div>

      <div className="section-grid-uneven">
        <div className="card">
          <div className="section-header">
            <h3>{editingId ? "Edit Goal" : "Create Goal"}</h3>
          </div>

          <form className="form-grid" onSubmit={handleGoalSubmit}>
            <div className="form-group">
              <label>Name</label>
              <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Emergency Fund" />
            </div>

            <div className="form-group">
              <label>Target Amount</label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={targetAmount}
                onChange={(e) => setTargetAmount(e.target.value)}
              />
            </div>

            <div className="form-group">
              <label>Target Date</label>
              <input type="date" value={targetDate} onChange={(e) => setTargetDate(e.target.value)} />
            </div>

            <div className="form-group">
              <label>Category</label>
              <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
                <option value="">No category</option>
                {expenseCategories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {sanitizeCategoryName(category.name)}
                    </option>
                  ))}
                </select>
            </div>

            <div className="form-group">
              <label>Linked Account</label>
              <select value={linkedAccountId} onChange={(e) => setLinkedAccountId(e.target.value)}>
                <option value="">No linked account</option>
                {eligibleAccounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {formatAccountDisplayName(account.name, account.type)}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Icon</label>
              <input value={icon} onChange={(e) => setIcon(e.target.value)} placeholder="Optional icon keyword" />
            </div>

            <div className="form-group form-group-full">
              <label>Color</label>
              <div className="color-grid">
                {palette.map((item) => (
                  <button
                    key={item}
                    type="button"
                    className={`color-swatch ${color === item ? "active" : ""}`}
                    style={{ backgroundColor: item }}
                    onClick={() => setColor(item)}
                    aria-label={`Select ${item}`}
                  />
                ))}
              </div>
            </div>

            <div className="form-actions">
              <button className="primary-btn" type="submit">
                {editingId ? "Update Goal" : "Create Goal"}
              </button>
              {editingId && (
                <button type="button" className="secondary-btn-inline" onClick={resetGoalForm}>
                  Cancel Edit
                </button>
              )}
            </div>
          </form>
        </div>

        <div className="card">
          <div className="section-header">
            <h3>Goal Actions</h3>
          </div>

          {goals.length === 0 ? (
            <p className="meta-text">Create your first goal to start adding contributions or withdrawals.</p>
          ) : (
            <form className="form-grid" onSubmit={handleGoalAction}>
              <div className="form-group form-group-full">
                <label>Goal</label>
                <select value={selectedGoalId} onChange={(e) => setSelectedGoalId(e.target.value)}>
                  <option value="">Select goal</option>
                  {goals.map((goal) => (
                    <option key={goal.id} value={goal.id}>
                      {goal.categoryName ? `${goal.name} (${goal.categoryName})` : goal.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group form-group-full">
                <div className="pill-switch" role="tablist" aria-label="Goal action mode">
                  <button
                    type="button"
                    className={`pill-switch-btn ${actionMode === "contribute" ? "active" : ""}`}
                    onClick={() => setActionMode("contribute")}
                  >
                    Contribute
                  </button>
                  <button
                    type="button"
                    className={`pill-switch-btn ${actionMode === "withdraw" ? "active" : ""}`}
                    onClick={() => setActionMode("withdraw")}
                  >
                    Withdraw
                  </button>
                </div>
                {selectedGoal?.linkedAccountName && (
                  <div className="helper-text">
                    Default linked account: {formatAccountDisplayName(
                      accountLookup[selectedGoal.linkedAccountId || ""]?.name || selectedGoal.linkedAccountName,
                      accountLookup[selectedGoal.linkedAccountId || ""]?.type
                    )}
                  </div>
                )}
              </div>

              <div className="form-group">
                <label>Amount</label>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  value={actionAmount}
                  onChange={(e) => setActionAmount(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label>Account</label>
                <select value={actionAccountId} onChange={(e) => setActionAccountId(e.target.value)}>
                  <option value="">Use linked account / no account</option>
                  {eligibleAccounts.map((account) => (
                    <option key={account.id} value={account.id}>
                      {formatAccountDisplayName(account.name, account.type)}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group form-group-full">
                <label>Note</label>
                <textarea
                  value={actionNote}
                  onChange={(e) => setActionNote(e.target.value)}
                  rows={3}
                  placeholder="Optional note"
                />
              </div>

              <div className="form-actions">
                <button className="primary-btn" type="submit" disabled={!selectedGoalId}>
                  {actionMode === "contribute" ? "Add Contribution" : "Withdraw Amount"}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>

      <div className="card">
        <h3>Goal Progress</h3>

        <div className="goal-list">
          {goals.map((goal) => (
            <div className="goal-item" key={goal.id}>
              <div className="goal-item-header">
                <div className="budget-title-group">
                  <span
                    className="color-chip"
                    style={{ backgroundColor: goal.color || palette[0] }}
                  />
                  <div>
                    <strong>{goal.name}</strong>
                    {goal.categoryName && (
                      <div className="meta-text">
                        Category: {goal.categoryName}
                      </div>
                    )}
                    <div className="meta-text">
                      Rs {goal.currentAmount.toLocaleString()} saved of Rs {goal.targetAmount.toLocaleString()}
                    </div>
                    {goal.targetDate && (
                      <div className="meta-text">
                        Target date: {goal.targetDate.slice(0, 10)}
                      </div>
                    )}
                    {goal.linkedAccountName && (
                      <div className="meta-text">
                        Linked account: {formatAccountDisplayName(
                          accountLookup[goal.linkedAccountId || ""]?.name || goal.linkedAccountName,
                          accountLookup[goal.linkedAccountId || ""]?.type
                        )}
                      </div>
                    )}
                  </div>
                </div>
                <div className={`status-pill ${goal.status}`}>
                  {goal.status === "completed" ? "Completed" : `${goal.progressPercent.toFixed(0)}%`}
                </div>
              </div>

              <div className="budget-bar">
                <div
                  className="goal-progress-fill"
                  style={{
                    width: `${Math.min(goal.progressPercent, 100)}%`,
                    background: `linear-gradient(135deg, ${goal.color || palette[0]} 0%, color-mix(in srgb, ${goal.color || palette[0]} 72%, #0f172a) 100%)`
                  }}
                />
              </div>

              <div className="goal-item-footer">
                <div className="meta-text">
                  Remaining: Rs {goal.remainingAmount.toLocaleString()}
                </div>
                <div className="row-actions">
                  <button className="link-btn" onClick={() => setSelectedGoalId(goal.id)}>
                    Select
                  </button>
                  <button className="link-btn" onClick={() => handleGoalEdit(goal)}>
                    Edit
                  </button>
                  <button className="link-btn danger" onClick={() => handleGoalDelete(goal)}>
                    Delete
                  </button>
                </div>
              </div>
            </div>
          ))}

          {goals.length === 0 && (
            <div className="goal-empty-state">
              <strong>No goals yet.</strong>
              <div className="meta-text">Create a savings goal to start tracking progress.</div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
