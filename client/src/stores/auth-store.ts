import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthUser, LoginRequest, RegisterRequest } from '../types/auth';
import apiClient from '../services/api-client';

interface AuthState {
  user: AuthUser | null;
  isLoading: boolean;
  error: string | null;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  clearError: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isLoading: false,
      error: null,

      login: async (data) => {
        set({ isLoading: true, error: null });
        try {
          const res = await apiClient.post<AuthUser>('/auth/login', data);
          localStorage.setItem('accessToken', res.data.accessToken);
          set({ user: res.data, isLoading: false });
        } catch {
          set({ error: 'Invalid credentials', isLoading: false });
        }
      },

      register: async (data) => {
        set({ isLoading: true, error: null });
        try {
          const res = await apiClient.post<AuthUser>('/auth/register', data);
          localStorage.setItem('accessToken', res.data.accessToken);
          set({ user: res.data, isLoading: false });
        } catch {
          set({ error: 'Registration failed', isLoading: false });
        }
      },

      logout: async () => {
        try {
          await apiClient.post('/auth/logout');
        } finally {
          localStorage.removeItem('accessToken');
          set({ user: null, error: null });
        }
      },

      clearError: () => set({ error: null }),
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({ user: state.user }),
    },
  ),
);
