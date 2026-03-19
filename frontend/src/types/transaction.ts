export type Transaction = {
  id: string;
  transactionNumber: number;
  userId: string;
  accountId?: string | null;
  accountName: string;
  goalId?: string | null;
  goalName?: string | null;
  categoryId?: string | null;
  type: string;
  amount: number;
  date: string;
  category?: string | null;
  merchant?: string | null;
  note?: string | null;
  paymentMethod?: string | null;
  tags: string[];
  createdAt: string;
  updatedAt: string;
};

export type TransactionListResponse = {
  items: Transaction[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  totalIncome: number;
  totalExpense: number;
};

export type CreateTransactionRequest = {
  accountId?: string;
  goalId?: string;
  categoryId?: string;
  type: string;
  amount: number;
  date: string;
  merchant?: string;
  note?: string;
  paymentMethod?: string;
  tags?: string[];
};

export type UpdateTransactionRequest = CreateTransactionRequest;
