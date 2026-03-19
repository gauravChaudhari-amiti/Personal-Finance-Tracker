export type Goal = {
  id: string;
  userId: string;
  name: string;
  targetAmount: number;
  currentAmount: number;
  remainingAmount: number;
  progressPercent: number;
  targetDate?: string | null;
  categoryId?: string | null;
  categoryName?: string | null;
  linkedAccountId?: string | null;
  linkedAccountName?: string | null;
  icon?: string | null;
  color?: string | null;
  status: "active" | "completed";
};

export type CreateGoalRequest = {
  name: string;
  targetAmount: number;
  targetDate?: string;
  categoryId?: string;
  linkedAccountId?: string;
  icon?: string;
  color?: string;
};

export type UpdateGoalRequest = CreateGoalRequest;

export type GoalActionRequest = {
  amount: number;
  accountId?: string;
  note?: string;
};
