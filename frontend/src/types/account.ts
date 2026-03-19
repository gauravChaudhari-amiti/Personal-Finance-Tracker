export type Account = {
  id: string;
  userId: string;
  name: string;
  type: string;
  categoryId?: string | null;
  categoryName?: string | null;
  openingBalance: number;
  currentBalance: number;
  creditLimit?: number | null;
  availableCredit?: number | null;
  institutionName?: string | null;
  lastUpdatedAt: string;
};

export type CreateAccountRequest = {
  name: string;
  type: string;
  categoryId?: string;
  openingBalance: number;
  creditLimit?: number;
  institutionName?: string;
};

export type UpdateAccountRequest = {
  name: string;
  type: string;
  categoryId?: string;
  creditLimit?: number;
  institutionName?: string;
};

export type TransferRequest = {
  sourceAccountId: string;
  destinationAccountId: string;
  amount: number;
  date?: string;
  note?: string;
};
