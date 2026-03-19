export type Budget = {
  id: string;
  userId: string;
  categoryId: string;
  categoryName: string;
  categoryColor?: string | null;
  month: number;
  year: number;
  amount: number;
  alertThresholdPercent: number;
  spentAmount: number;
  remainingAmount: number;
  progressPercent: number;
  status: "safe" | "warning" | "over" | "critical";
};

export type CreateBudgetRequest = {
  categoryId: string;
  month: number;
  year: number;
  amount: number;
  alertThresholdPercent: number;
};

export type UpdateBudgetRequest = CreateBudgetRequest;

export type DuplicateBudgetRequest = {
  sourceMonth: number;
  sourceYear: number;
  targetMonth: number;
  targetYear: number;
};
