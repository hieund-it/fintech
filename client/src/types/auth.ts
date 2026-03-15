export interface AuthUser {
  userId: string;
  email: string;
  displayName: string;
  accessToken: string;
  avatarUrl?: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}
