export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  displayName: string;
  email: string;
  password: string;
};

export type AuthActionResponse = {
  message: string;
  previewUrl?: string;
};

export type ForgotPasswordRequest = {
  email: string;
};

export type ResetPasswordRequest = {
  token: string;
  password: string;
};

export type EmailTokenRequest = {
  token: string;
};

export type GoogleLoginRequest = {
  credential: string;
};

export type GoogleAuthConfigResponse = {
  clientId?: string;
  enabled: boolean;
};

export type LoginResponse = {
  userNumber: number;
  email: string;
  displayName: string;
  role: string;
};

export type AuthUser = {
  userNumber: number;
  email: string;
  displayName: string;
  role: string;
};
