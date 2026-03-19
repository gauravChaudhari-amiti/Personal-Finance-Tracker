import { api } from "./api";
import type { LoginRequest, LoginResponse, RegisterRequest } from "../types/auth";

export const authService = {
  async login(payload: LoginRequest) {
    const response = await api.post<LoginResponse>("/auth/login", payload);
    return response.data;
  },

  async register(payload: RegisterRequest) {
    const response = await api.post<LoginResponse>("/auth/register", payload);
    return response.data;
  }
};
