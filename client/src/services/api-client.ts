import axios from 'axios';
import { useAuthStore } from '../stores/auth-store';

const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true, // send HttpOnly refresh-token cookie
  headers: { 'Content-Type': 'application/json' },
});

// Attach access token from in-memory Zustand store (never from localStorage)
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().user?.accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
