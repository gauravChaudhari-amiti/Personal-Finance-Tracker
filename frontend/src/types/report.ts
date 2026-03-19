export type ReportSummary = {
  totalIncome: number;
  totalExpense: number;
  netCashFlow: number;
  transactionCount: number;
};

export type CategorySpendReportItem = {
  categoryName: string;
  amount: number;
};

export type IncomeExpenseTrendPoint = {
  label: string;
  income: number;
  expense: number;
  net: number;
};

export type AccountBalanceTrendPoint = {
  label: string;
  balance: number;
};

export type AccountPosition = {
  accountId: string;
  accountName: string;
  accountType: string;
  currentBalance: number;
};

export type ReportResponse = {
  summary: ReportSummary;
  categorySpend: CategorySpendReportItem[];
  incomeExpenseTrend: IncomeExpenseTrendPoint[];
  accountBalanceTrend: AccountBalanceTrendPoint[];
  accountPositions: AccountPosition[];
};

export type ReportFilters = {
  from?: string;
  to?: string;
  accountId?: string;
  categoryId?: string;
  type?: string;
};
