import { api } from "./api";
import type {
  CreateTransactionRequest,
  Transaction,
  TransactionListResponse,
  UpdateTransactionRequest
} from "../types/transaction";

type TransactionFilters = {
  type?: string;
  accountId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
};

export const transactionService = {
  async getAll(filters?: TransactionFilters) {
    const response = await api.get<TransactionListResponse>("/transactions", {
      params: filters
    });
    return response.data;
  },

  async create(payload: CreateTransactionRequest) {
    const response = await api.post<Transaction>("/transactions", payload);
    return response.data;
  },

  async update(id: string, payload: UpdateTransactionRequest) {
    const response = await api.put<Transaction>(`/transactions/${id}`, payload);
    return response.data;
  },

  async delete(id: string) {
    const response = await api.delete<{ message: string }>(`/transactions/${id}`);
    return response.data;
  }
};
