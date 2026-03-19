export type RecurringTransaction = {
  id: string;
  userId: string;
  title: string;
  type: "income" | "expense";
  amount: number;
  categoryId: string;
  categoryName: string;
  accountId: string;
  accountName: string;
  frequency: "daily" | "weekly" | "monthly" | "yearly";
  startDate: string;
  endDate?: string | null;
  nextRunDate: string;
  autoCreateTransaction: boolean;
  isPaused: boolean;
  lastRunAt?: string | null;
};

export type CreateRecurringTransactionRequest = {
  title: string;
  type: "income" | "expense";
  amount: number;
  categoryId: string;
  accountId: string;
  frequency: "daily" | "weekly" | "monthly" | "yearly";
  startDate: string;
  endDate?: string;
  nextRunDate?: string;
  autoCreateTransaction: boolean;
  isPaused: boolean;
};

export type UpdateRecurringTransactionRequest = CreateRecurringTransactionRequest;
