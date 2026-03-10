import axios from 'axios';

const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true, // send HttpOnly refresh-token cookie
  headers: { 'Content-Type': 'application/json' },
});

// Attach access token from localStorage on every request
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
