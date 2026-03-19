import { api } from "./api";
import type {
  CreateRecurringTransactionRequest,
  RecurringTransaction,
  UpdateRecurringTransactionRequest
} from "../types/recurring";

export const recurringService = {
  async getAll() {
    const response = await api.get<RecurringTransaction[]>("/recurring");
    return response.data;
  },

  async create(payload: CreateRecurringTransactionRequest) {
    const response = await api.post<RecurringTransaction>("/recurring", payload);
    return response.data;
  },

  async update(id: string, payload: UpdateRecurringTransactionRequest) {
    const response = await api.put<RecurringTransaction>(`/recurring/${id}`, payload);
    return response.data;
  },

  async delete(id: string) {
    const response = await api.delete<{ message: string }>(`/recurring/${id}`);
    return response.data;
  }
};
