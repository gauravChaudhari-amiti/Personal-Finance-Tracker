import { api } from "./api";
import type {
  CreateGoalRequest,
  Goal,
  GoalActionRequest,
  UpdateGoalRequest
} from "../types/goal";

export const goalService = {
  async getAll() {
    const response = await api.get<Goal[]>("/goals");
    return response.data;
  },

  async create(payload: CreateGoalRequest) {
    const response = await api.post<Goal>("/goals", payload);
    return response.data;
  },

  async update(id: string, payload: UpdateGoalRequest) {
    const response = await api.put<Goal>(`/goals/${id}`, payload);
    return response.data;
  },

  async delete(id: string) {
    const response = await api.delete<{ message: string }>(`/goals/${id}`);
    return response.data;
  },

  async contribute(id: string, payload: GoalActionRequest) {
    const response = await api.post<Goal>(`/goals/${id}/contribute`, payload);
    return response.data;
  },

  async withdraw(id: string, payload: GoalActionRequest) {
    const response = await api.post<Goal>(`/goals/${id}/withdraw`, payload);
    return response.data;
  }
};
