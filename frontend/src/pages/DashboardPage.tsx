import { useEffect, useState } from "react";
import { dashboardService } from "../services/dashboardService";
import type { DashboardSummary } from "../types/dashboard";

const isOutgoingType = (transactionType: string) =>
  transactionType === "expense" ||
  transactionType === "transfer-out" ||
  transactionType === "self-transfer-out" ||
  transactionType === "card-settlement-out";

const MS_PER_DAY = 24 * 60 * 60 * 1000;

const getDaysUntilDue = (dueDate: string) => {
  const today = new Date();
  const startOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  const due = new Date(`${dueDate}T00:00:00`);

  return Math.ceil((due.getTime() - startOfToday.getTime()) / MS_PER_DAY);
};

const getBillUrgency = (dueDate: string) => {
  const daysUntilDue = getDaysUntilDue(dueDate);

  if (daysUntilDue <= 3) {
    return {
      tone: "critical" as const,
      label:
        daysUntilDue < 0
          ? `Overdue by ${Math.abs(daysUntilDue)} day${Math.abs(daysUntilDue) === 1 ? "" : "s"}`
          : daysUntilDue === 0
            ? "Due today"
            : `Due in ${daysUntilDue} day${daysUntilDue === 1 ? "" : "s"}`
    };
  }

  if (daysUntilDue <= 7) {
    return {
      tone: "warning" as const,
      label: `Due in ${daysUntilDue} day${daysUntilDue === 1 ? "" : "s"}`
    };
  }

  return {
    tone: "normal" as const,
    label: `Due in ${daysUntilDue} day${daysUntilDue === 1 ? "" : "s"}`
  };
};

export default function DashboardPage() {
  const [data, setData] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const result = await dashboardService.getSummary();
        setData(result);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, []);

  if (loading) return <div>Loading dashboard...</div>;
  if (!data) return <div>Failed to load dashboard.</div>;

  const netFlow = data.currentMonthIncome - data.currentMonthExpense;
  const hasUpcomingBills = data.upcomingBills.length > 0;
  const recentTransactionCount = data.recentTransactions.length;

  return (
    <div>
      <div className="page-hero">
        <div className="page-hero-copy">
          <div className="page-kicker">Financial command center</div>
          <h1 className="page-title">Dashboard</h1>
          <p className="page-description">
            Watch your monthly flow, spot upcoming bills, and keep recent activity in one focused view.
          </p>
        </div>
        <div className="page-hero-panel">
          <div className="hero-panel-label">This month at a glance</div>
          <div className="hero-panel-value">Rs {netFlow.toLocaleString()}</div>
          <div className="hero-panel-text">
            {netFlow >= 0 ? "Positive monthly cash flow so far." : "You are currently spending beyond income this month."}
          </div>
          <div className="hero-chip-row">
            <span className="hero-chip">{recentTransactionCount} recent entries</span>
            <span className="hero-chip">{hasUpcomingBills ? `${data.upcomingBills.length} upcoming bills` : "No upcoming bills"}</span>
          </div>
        </div>
      </div>

      <div className="cards-grid">
        <div className="card stat-card income-card">
          <h3>Current Month Income</h3>
          <div className="big-number">Rs {data.currentMonthIncome.toLocaleString()}</div>
          <div className="stat-footnote">Money flowing in this month</div>
        </div>

        <div className="card stat-card expense-card">
          <h3>Current Month Expense</h3>
          <div className="big-number">Rs {data.currentMonthExpense.toLocaleString()}</div>
          <div className="stat-footnote">Tracked spending so far</div>
        </div>

        <div className="card stat-card balance-card">
          <h3>Net Balance</h3>
          <div className="big-number">Rs {data.netBalance.toLocaleString()}</div>
          <div className="stat-footnote">Across your connected accounts</div>
        </div>
      </div>

      <div className="section-grid">
        <div className="card section-card">
          <div className="section-heading-row">
            <h3>Recent Transactions</h3>
            <span className="section-counter">{recentTransactionCount}</span>
          </div>
          <div className="list">
            {data.recentTransactions.map((item) => (
              <div className="list-item feature-list-item" key={item.id}>
                <div className="list-item-copy">
                  <strong>{item.title}</strong>
                  <div className="meta-text">{item.category}</div>
                  <div className="list-date-pill">{item.date}</div>
                </div>
                <div className={`list-amount ${isOutgoingType(item.type) ? "amount-expense" : "amount-income"}`}>
                  {isOutgoingType(item.type) ? "-" : "+"}Rs {item.amount.toLocaleString()}
                </div>
              </div>
            ))}
            {data.recentTransactions.length === 0 && (
              <div className="empty-state-card">
                <strong>No transactions yet.</strong>
                <div className="meta-text">Add your first entry to start building a useful financial timeline.</div>
              </div>
            )}
          </div>
        </div>

        <div className="card section-card">
          <div className="section-heading-row">
            <h3>Upcoming Bills</h3>
            <span className="section-counter">{data.upcomingBills.length}</span>
          </div>
          <div className="list">
            {data.upcomingBills.map((item) => {
              const urgency = getBillUrgency(item.dueDate);

              return (
                <div className={`list-item feature-list-item bill-list-item bill-${urgency.tone}`} key={item.id}>
                  <div className="list-item-copy">
                    <strong>{item.title}</strong>
                    <div className="meta-text">Due date</div>
                    <div className={`list-date-pill bill-date-pill bill-${urgency.tone}`}>{item.dueDate}</div>
                    {urgency.tone !== "normal" && (
                      <div className={`bill-urgency-chip bill-${urgency.tone}`}>{urgency.label}</div>
                    )}
                  </div>
                  <div className="list-amount amount-expense">Rs {item.amount.toLocaleString()}</div>
                </div>
              );
            })}
            {data.upcomingBills.length === 0 && (
              <div className="empty-state-card">
                <strong>No upcoming recurring bills yet.</strong>
                <div className="meta-text">Add recurring items to turn this area into a useful bill reminder board.</div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
