import { api } from "./api";
import type {
  AuthActionResponse,
  EmailTokenRequest,
  ForgotPasswordRequest,
  GoogleAuthConfigResponse,
  GoogleLoginRequest,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  ResetPasswordRequest
} from "../types/auth";

export const authService = {
  async login(payload: LoginRequest) {
    const response = await api.post<LoginResponse>("/auth/login", payload);
    return response.data;
  },

  async me() {
    const response = await api.get<LoginResponse>("/auth/me");
    return response.data;
  },

  async register(payload: RegisterRequest) {
    const response = await api.post<AuthActionResponse>("/auth/register", payload);
    return response.data;
  },

  async resendVerification(payload: ForgotPasswordRequest) {
    const response = await api.post<AuthActionResponse>("/auth/resend-verification", payload);
    return response.data;
  },

  async verifyEmail(payload: EmailTokenRequest) {
    const response = await api.post<AuthActionResponse>("/auth/verify-email", payload);
    return response.data;
  },

  async forgotPassword(payload: ForgotPasswordRequest) {
    const response = await api.post<AuthActionResponse>("/auth/forgot-password", payload);
    return response.data;
  },

  async resetPassword(payload: ResetPasswordRequest) {
    const response = await api.post<AuthActionResponse>("/auth/reset-password", payload);
    return response.data;
  },

  async loginWithGoogle(payload: GoogleLoginRequest) {
    const response = await api.post<LoginResponse>("/auth/google", payload);
    return response.data;
  },

  async getGoogleConfig() {
    const response = await api.get<GoogleAuthConfigResponse>("/auth/google-config");
    return response.data;
  },

  async logout() {
    const response = await api.post<AuthActionResponse>("/auth/logout");
    return response.data;
  }
};
