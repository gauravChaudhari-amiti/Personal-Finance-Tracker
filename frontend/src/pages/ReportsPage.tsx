import { useEffect, useMemo, useState } from "react";
import { accountService } from "../services/accountService";
import { categoryService } from "../services/categoryService";
import { reportService } from "../services/reportService";
import type { Account } from "../types/account";
import type { Category } from "../types/category";
import type { ReportFilters, ReportResponse } from "../types/report";
import { formatAccountDisplayName } from "../utils/accountDisplay";
import { hasVisibleCategoryName, sanitizeCategoryName } from "../utils/categoryName";

const today = new Date();
const firstDayOfMonth = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().slice(0, 10);
const todayIso = today.toISOString().slice(0, 10);

export default function ReportsPage() {
  const [report, setReport] = useState<ReportResponse | null>(null);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [from, setFrom] = useState(firstDayOfMonth);
  const [to, setTo] = useState(todayIso);
  const [accountId, setAccountId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [type, setType] = useState("");

  const maxCategorySpend = useMemo(
    () => Math.max(...(report?.categorySpend.map((item) => item.amount) || [0])),
    [report]
  );

  const maxTrendAmount = useMemo(() => {
    const values = [
      ...(report?.incomeExpenseTrend.map((item) => item.income) || []),
      ...(report?.incomeExpenseTrend.map((item) => item.expense) || []),
      ...(report?.accountBalanceTrend.map((item) => Math.abs(item.balance)) || [])
    ];

    return Math.max(...values, 0);
  }, [report]);

  const activeFilters: ReportFilters = useMemo(
    () => ({
      from,
      to,
      accountId: accountId || undefined,
      categoryId: categoryId || undefined,
      type: type || undefined
    }),
    [from, to, accountId, categoryId, type]
  );

  const loadReferenceData = async () => {
    const [accountData, categoryData] = await Promise.all([
      accountService.getAll(),
      categoryService.getAll()
    ]);

    setAccounts(accountData);
    setCategories(
      categoryData.filter(
        (category) => !category.isArchived && hasVisibleCategoryName(category.name)
      )
    );
  };

  const loadReport = async (filters: ReportFilters) => {
    const data = await reportService.getSummary(filters);
    setReport(data);
  };

  useEffect(() => {
    const initialLoad = async () => {
      try {
        setError("");
        await loadReferenceData();
        await loadReport(activeFilters);
      } catch (err: any) {
        setError(err?.response?.data?.message || "Failed to load reports.");
      } finally {
        setLoading(false);
      }
    };

    initialLoad();
  }, []);

  const handleApplyFilters = async () => {
    try {
      setError("");
      await loadReport(activeFilters);
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to apply report filters.");
    }
  };

  const handleExport = async () => {
    try {
      setError("");
      const blob = await reportService.exportCsv(activeFilters);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `finance-report-${todayIso}.csv`;
      link.click();
      window.URL.revokeObjectURL(url);
    } catch (err: any) {
      setError(err?.response?.data?.message || "Failed to export CSV.");
    }
  };

  if (loading) return <div>Loading reports...</div>;
  if (!report) return <div>Failed to load reports.</div>;

  return (
    <div>
      <h1 className="page-title">Reports</h1>

      {error && <div className="error-banner">{error}</div>}

      <div className="card report-filter-card">
        <div className="section-header">
          <h3>Filters</h3>
          <button className="secondary-btn-inline" type="button" onClick={handleExport}>
            Export CSV
          </button>
        </div>

        <div className="form-grid">
          <div className="form-group">
            <label>From</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>

          <div className="form-group">
            <label>To</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>

          <div className="form-group">
            <label>Account</label>
            <select value={accountId} onChange={(e) => setAccountId(e.target.value)}>
              <option value="">All Accounts</option>
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {formatAccountDisplayName(account.name, account.type)}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Category</label>
            <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
              <option value="">All Categories</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {sanitizeCategoryName(category.name)}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Type</label>
            <select value={type} onChange={(e) => setType(e.target.value)}>
              <option value="">All Types</option>
              <option value="income">Income</option>
              <option value="expense">Expense</option>
            </select>
          </div>

          <div className="form-actions">
            <button className="primary-btn" type="button" onClick={handleApplyFilters}>
              Apply Filters
            </button>
          </div>
        </div>
      </div>

      <div className="cards-grid">
        <div className="card">
          <h3>Total Income</h3>
          <div className="big-number">Rs {report.summary.totalIncome.toLocaleString()}</div>
        </div>
        <div className="card">
          <h3>Total Expense</h3>
          <div className="big-number">Rs {report.summary.totalExpense.toLocaleString()}</div>
        </div>
        <div className="card">
          <h3>Net Cash Flow</h3>
          <div className="big-number">Rs {report.summary.netCashFlow.toLocaleString()}</div>
          <div className="meta-text">{report.summary.transactionCount} transactions in range</div>
        </div>
      </div>

      <div className="section-grid">
        <div className="card">
          <h3>Category Spend</h3>
          <div className="report-bar-list">
            {report.categorySpend.map((item) => (
              <div className="report-bar-row" key={item.categoryName}>
                <div className="report-bar-label">
                  <strong>{item.categoryName}</strong>
                  <span>Rs {item.amount.toLocaleString()}</span>
                </div>
                <div className="report-bar-track">
                  <div
                    className="report-bar-fill expense"
                    style={{ width: `${maxCategorySpend === 0 ? 0 : (item.amount / maxCategorySpend) * 100}%` }}
                  />
                </div>
              </div>
            ))}
            {report.categorySpend.length === 0 && <div className="meta-text">No spending data for this range.</div>}
          </div>
        </div>

        <div className="card">
          <h3>Current Account Positions</h3>
          <div className="list">
            {report.accountPositions.map((account) => (
              <div className="list-item" key={account.accountId}>
                <div>
                  <strong>{formatAccountDisplayName(account.accountName, account.accountType)}</strong>
                </div>
                <div className="meta-text">Rs {account.currentBalance.toLocaleString()}</div>
              </div>
            ))}
            {report.accountPositions.length === 0 && <div className="meta-text">No account data for this filter.</div>}
          </div>
        </div>
      </div>

      <div className="section-grid">
        <div className="card">
          <h3>Income vs Expense Trend</h3>
          <div className="report-bar-list">
            {report.incomeExpenseTrend.map((item) => (
              <div className="report-trend-group" key={item.label}>
                <div className="report-bar-label">
                  <strong>{item.label}</strong>
                  <span>Net: Rs {item.net.toLocaleString()}</span>
                </div>
                <div className="report-dual-bar">
                  <div className="report-bar-track">
                    <div
                      className="report-bar-fill income"
                      style={{ width: `${maxTrendAmount === 0 ? 0 : (item.income / maxTrendAmount) * 100}%` }}
                    />
                  </div>
                  <div className="report-bar-track">
                    <div
                      className="report-bar-fill expense"
                      style={{ width: `${maxTrendAmount === 0 ? 0 : (item.expense / maxTrendAmount) * 100}%` }}
                    />
                  </div>
                </div>
                <div className="report-trend-caption">
                  <span>Income: Rs {item.income.toLocaleString()}</span>
                  <span>Expense: Rs {item.expense.toLocaleString()}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="card">
          <h3>Balance Trend</h3>
          <div className="report-bar-list">
            {report.accountBalanceTrend.map((item) => (
              <div className="report-bar-row" key={item.label}>
                <div className="report-bar-label">
                  <strong>{item.label}</strong>
                  <span>Rs {item.balance.toLocaleString()}</span>
                </div>
                <div className="report-bar-track">
                  <div
                    className="report-bar-fill balance"
                    style={{ width: `${maxTrendAmount === 0 ? 0 : (Math.abs(item.balance) / maxTrendAmount) * 100}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
