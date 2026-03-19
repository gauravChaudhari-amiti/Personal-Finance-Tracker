export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  displayName: string;
  email: string;
  password: string;
};

export type LoginResponse = {
  token: string;
  userNumber: number;
  email: string;
  displayName: string;
  role: string;
};

export type AuthUser = {
  token: string;
  userNumber: number;
  email: string;
  displayName: string;
  role: string;
};
