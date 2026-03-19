export type RecentTransaction = {
  id: string;
  title: string;
  category: string;
  type: string;
  amount: number;
  date: string;
};

export type UpcomingBill = {
  id: string;
  title: string;
  amount: number;
  dueDate: string;
};

export type DashboardSummary = {
  currentMonthIncome: number;
  currentMonthExpense: number;
  netBalance: number;
  recentTransactions: RecentTransaction[];
  upcomingBills: UpcomingBill[];
};
