import { api } from "./api";
import type {
  Account,
  CreateAccountRequest,
  TransferRequest,
  UpdateAccountRequest
} from "../types/account";

export const accountService = {
  async getAll() {
    const response = await api.get<Account[]>("/accounts");
    return response.data;
  },

  async create(payload: CreateAccountRequest) {
    const response = await api.post<Account>("/accounts", payload);
    return response.data;
  },

  async update(id: string, payload: UpdateAccountRequest) {
    const response = await api.put<Account>(`/accounts/${id}`, payload);
    return response.data;
  },

  async delete(id: string) {
    const response = await api.delete<{ message: string }>(`/accounts/${id}`);
    return response.data;
  },

  async transfer(payload: TransferRequest) {
    const response = await api.post<{ message: string }>("/accounts/transfer", payload);
    return response.data;
  }
};
