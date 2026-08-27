export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  email: string;
}

export interface CurrentUser {
  id: number;
  email: string;
  lastLoginAt: string | null;
}
