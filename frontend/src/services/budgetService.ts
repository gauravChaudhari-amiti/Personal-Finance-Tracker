import { api } from "./api";
import type {
  Budget,
  CreateBudgetRequest,
  DuplicateBudgetRequest,
  UpdateBudgetRequest
} from "../types/budget";

export const budgetService = {
  async getAll(month: number, year: number) {
    const response = await api.get<Budget[]>("/budgets", {
      params: { month, year }
    });
    return response.data;
  },

  async create(payload: CreateBudgetRequest) {
    const response = await api.post<Budget>("/budgets", payload);
    return response.data;
  },

  async update(id: string, payload: UpdateBudgetRequest) {
    const response = await api.put<Budget>(`/budgets/${id}`, payload);
    return response.data;
  },

  async delete(id: string) {
    const response = await api.delete<{ message: string }>(`/budgets/${id}`);
    return response.data;
  },

  async duplicate(payload: DuplicateBudgetRequest) {
    const response = await api.post<Budget[]>("/budgets/duplicate", payload);
    return response.data;
  }
};
