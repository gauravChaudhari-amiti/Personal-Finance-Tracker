import { useEffect, useMemo, useState } from "react";
import { accountService } from "../services/accountService";
import { categoryService } from "../services/categoryService";
import type { Account } from "../types/account";
import type { Category } from "../types/category";
import { formatAccountDisplayName } from "../utils/accountDisplay";
import { hasVisibleCategoryName, sanitizeCategoryName } from "../utils/categoryName";

const accountTypes = ["Bank Account", "Credit Card", "Cash Wallet", "Savings Account", "Fund"];

export default function AccountsPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [editingId, setEditingId] = useState<string | null>(null);

  const [name, setName] = useState("");
  const [type, setType] = useState("Bank Account");
  const [categoryId, setCategoryId] = useState("");
  const [openingBalance, setOpeningBalance] = useState("0");
  const [creditLimit, setCreditLimit] = useState("");
  const [institutionName, setInstitutionName] = useState("");

  const [transferSourceAccountId, setTransferSourceAccountId] = useState("");
  const [transferDestinationAccountId, setTransferDestinationAccountId] = useState("");
  const [transferAmount, setTransferAmount] = useState("");
  const [transferNote, setTransferNote] = useState("");

  const netWorth = useMemo(
    () =>
      accounts.reduce((sum, account) => {
        if (account.type === "Credit Card") {
          return sum - account.currentBalance;
        }

        return sum + account.currentBalance;
      }, 0),
    [accounts]
  );

  const totalAvailableCredit = useMemo(
    () =>
      accounts
        .filter((account) => account.type === "Credit Card")
        .reduce((sum, account) => sum + (account.availableCredit ?? 0), 0),
    [accounts]
  );

  const isCreditCard = type === "Credit Card";
  const isFund = type === "Fund";
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
  const selectedFundCategory = expenseCategories.find((category) => category.id === categoryId) || null;
  const accountPreviewLabel = useMemo(
    () =>
      formatAccountDisplayName(
        name || (isFund && selectedFundCategory ? `${selectedFundCategory.name} Fund` : "Account Name"),
        type
      ),
    [name, type, isFund, selectedFundCategory]
  );
  const selectedSourceAccount = accounts.find((account) => account.id === transferSourceAccountId) || null;
  const selectedDestinationAccount =
    accounts.find((account) => account.id === transferDestinationAccountId) || null;

  const settlementActionLabel = useMemo(() => {
    if (
      selectedDestinationAccount?.type === "Credit Card" &&
      selectedSourceAccount?.type !== "Credit Card"
    ) {
      const numericAmount = Number(transferAmount);
      const dueAmount = selectedDestinationAccount.currentBalance;

      if (numericAmount > 0 && dueAmount > 0) {
        if (numericAmount === dueAmount) {
          return "Pay Off";
        }

        if (numericAmount < dueAmount) {
          return "Pay Down";
        }
      }

      return "Pay Card";
    }

    return "Self Transfer";
  }, [selectedDestinationAccount, selectedSourceAccount, transferAmount]);

  const loadAccounts = async () => {
    try {
      setError("");
      const [data, categoryData] = await Promise.all([
        accountService.getAll(),
        categoryService.getAll({ type: "expense" })
      ]);
      setAccounts(data);
      setCategories(categoryData.filter((category) => category.name !== "Whole Month"));

      if (data.length > 0) {
        setTransferSourceAccountId((prev) => prev || data[0].id);
        setTransferDestinationAccountId((prev) => prev || data[Math.min(1, data.length - 1)].id);
      }

      if (categoryData.length > 0 && !categoryId) {
        const firstCategory = categoryData.find((category) => category.name !== "Whole Month");
        setCategoryId(firstCategory?.id || "");
      }
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to load accounts.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAccounts();
  }, []);

  useEffect(() => {
    if (type === "Credit Card") {
      setOpeningBalance("0");
      setCategoryId("");
      return;
    }

    setCreditLimit("");
    if (type !== "Fund") {
      setCategoryId("");
    } else if (!categoryId && expenseCategories.length > 0) {
      setCategoryId(expenseCategories[0].id);
    }
  }, [type, categoryId, expenseCategories]);

  const resetForm = () => {
    setEditingId(null);
    setName("");
    setType("Bank Account");
    setCategoryId("");
    setOpeningBalance("0");
    setCreditLimit("");
    setInstitutionName("");
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    try {
      const resolvedName = name.trim() || (isFund && selectedFundCategory ? `${selectedFundCategory.name} Fund` : name);

      if (editingId) {
        await accountService.update(editingId, {
          name: resolvedName,
          type,
          categoryId: isFund ? categoryId : undefined,
          creditLimit: isCreditCard ? Number(creditLimit) : undefined,
          institutionName
        });
      } else {
        await accountService.create({
          name: resolvedName,
          type,
          categoryId: isFund ? categoryId : undefined,
          openingBalance: isCreditCard ? 0 : Number(openingBalance),
          creditLimit: isCreditCard ? Number(creditLimit) : undefined,
          institutionName
        });
      }

      resetForm();
      await loadAccounts();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to save account.");
    }
  };

  const handleEdit = (account: Account) => {
    setEditingId(account.id);
    setName(account.name);
    setType(account.type);
    setCategoryId(account.categoryId || "");
    setOpeningBalance(account.openingBalance.toString());
    setCreditLimit(account.creditLimit?.toString() || "");
    setInstitutionName(account.institutionName || "");
  };

  const handleDelete = async (id: string) => {
    const confirmed = window.confirm("Delete this account?");
    if (!confirmed) return;

    try {
      await accountService.delete(id);
      await loadAccounts();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to delete account.");
    }
  };

  const handleTransfer = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (selectedSourceAccount?.type === "Credit Card" && selectedDestinationAccount?.type !== "Credit Card") {
      setError("Self transfer from a credit card to another account is not supported.");
      return;
    }

    try {
      await accountService.transfer({
        sourceAccountId: transferSourceAccountId,
        destinationAccountId: transferDestinationAccountId,
        amount: Number(transferAmount),
        date: new Date().toISOString(),
        note: transferNote
      });

      setTransferAmount("");
      setTransferNote("");
      await loadAccounts();
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to transfer funds.");
    }
  };

  if (loading) return <div>Loading accounts...</div>;

  return (
    <div>
      <h1 className="page-title">Accounts</h1>

      {error && <div className="error-banner">{error}</div>}

      <div className="cards-grid">
        <div className="card">
          <h3>Net Worth</h3>
          <div className="big-number">Rs {netWorth.toLocaleString()}</div>
        </div>
        <div className="card">
          <h3>Total Accounts</h3>
          <div className="big-number">{accounts.length}</div>
        </div>
        <div className="card">
          <h3>Available Credit</h3>
          <div className="big-number">Rs {totalAvailableCredit.toLocaleString()}</div>
        </div>
      </div>

      <div className="section-grid-uneven">
        <div className="card">
          <h3>{editingId ? "Edit Account" : "Add Account"}</h3>

          <form className="form-grid" onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Account Name</label>
              <input value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. HDFC Bank" />
              <div className="helper-text">Shown as: {accountPreviewLabel}</div>
            </div>

            <div className="form-group">
              <label>Type</label>
              <select value={type} onChange={(e) => setType(e.target.value)}>
                {accountTypes.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </div>

            {isFund && (
              <div className="form-group">
                <label>Fund Category</label>
                <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
                  <option value="">Select category</option>
                  {expenseCategories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {sanitizeCategoryName(category.name)}
                    </option>
                  ))}
                </select>
              </div>
            )}

            {!editingId && !isCreditCard && (
              <div className="form-group">
                <label>{isFund ? "Fund Balance" : "Opening Balance"}</label>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  value={openingBalance}
                  onChange={(e) => setOpeningBalance(e.target.value)}
                />
              </div>
            )}

            {isCreditCard && (
              <div className="form-group">
                <label>Credit Limit</label>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  value={creditLimit}
                  onChange={(e) => setCreditLimit(e.target.value)}
                />
              </div>
            )}

            <div className="form-group">
              <label>Institution</label>
              <input
                value={institutionName}
                onChange={(e) => setInstitutionName(e.target.value)}
                placeholder="Optional"
              />
            </div>

            <div className="form-actions">
              <button type="submit" className="primary-btn">
                {editingId ? "Update Account" : "Create Account"}
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
          <h3>{settlementActionLabel}</h3>

          <form className="form-grid" onSubmit={handleTransfer}>
            <div className="form-group">
              <label>Source Account</label>
              <select
                value={transferSourceAccountId}
                onChange={(e) => setTransferSourceAccountId(e.target.value)}
              >
                <option value="">Select source</option>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {formatAccountDisplayName(account.name, account.type)} (Rs {account.currentBalance.toLocaleString()})
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Destination Account</label>
              <select
                value={transferDestinationAccountId}
                onChange={(e) => setTransferDestinationAccountId(e.target.value)}
              >
                <option value="">Select destination</option>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {formatAccountDisplayName(account.name, account.type)} (Rs {account.currentBalance.toLocaleString()})
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Amount</label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={transferAmount}
                onChange={(e) => setTransferAmount(e.target.value)}
              />
              {selectedDestinationAccount?.type === "Credit Card" && (
                <div className="helper-text">
                  Current due: Rs {selectedDestinationAccount.currentBalance.toLocaleString()}
                </div>
              )}
            </div>

            <div className="form-group">
              <label>Note</label>
              <input
                value={transferNote}
                onChange={(e) => setTransferNote(e.target.value)}
                placeholder="Optional note"
              />
            </div>

            <button type="submit" className="primary-btn">
              {settlementActionLabel}
            </button>
          </form>
        </div>
      </div>

      <div className="card">
        <h3>All Accounts</h3>

        <div className="table-wrapper">
          <table className="app-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Type</th>
                <th>Category</th>
                <th>Institution</th>
                <th>Base / Limit</th>
                <th>Current Position</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {accounts.map((account) => (
                <tr key={account.id}>
                  <td>{formatAccountDisplayName(account.name, account.type)}</td>
                  <td>{account.type}</td>
                  <td>{account.categoryName || "-"}</td>
                  <td>{account.institutionName || "-"}</td>
                  <td>
                    {account.type === "Credit Card"
                      ? `Rs ${(account.creditLimit ?? 0).toLocaleString()} limit`
                      : `Rs ${account.openingBalance.toLocaleString()}`}
                  </td>
                  <td>
                    {account.type === "Credit Card"
                      ? `Rs ${account.currentBalance.toLocaleString()} due`
                      : `Rs ${account.currentBalance.toLocaleString()}`}
                  </td>
                  <td>
                    <div className="row-actions">
                      <button className="link-btn" onClick={() => handleEdit(account)}>
                        Edit
                      </button>
                      <button className="link-btn danger" onClick={() => handleDelete(account.id)}>
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}

              {accounts.length === 0 && (
                <tr>
                  <td colSpan={7}>No accounts yet.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
