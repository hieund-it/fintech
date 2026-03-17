import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useAuthStore } from './auth-store';
import apiClient from '../services/api-client';

vi.mock('../services/api-client', () => ({
  default: {
    post: vi.fn(),
    interceptors: { request: { use: vi.fn() } },
  },
}));

const mockedPost = vi.mocked(apiClient.post);

beforeEach(() => {
  useAuthStore.setState({ user: null, isLoading: false, error: null });
  vi.clearAllMocks();
  localStorage.clear();
});

describe('auth-store', () => {
  it('login: sets user and token on success', async () => {
    mockedPost.mockResolvedValueOnce({
      data: { userId: '1', email: 'a@b.com', displayName: 'Alice', accessToken: 'tok' },
    });

    await useAuthStore.getState().login({ email: 'a@b.com', password: 'pass' });

    const { user, error } = useAuthStore.getState();
    expect(user?.email).toBe('a@b.com');
    expect(user?.accessToken).toBe('tok');
    expect(error).toBeNull();
  });

  it('login: sets error on failure', async () => {
    mockedPost.mockRejectedValueOnce(new Error('Unauthorized'));

    await useAuthStore.getState().login({ email: 'a@b.com', password: 'wrong' });

    const { user, error } = useAuthStore.getState();
    expect(user).toBeNull();
    expect(error).toBe('Invalid credentials');
  });

  it('logout: clears user and token', async () => {
    useAuthStore.setState({
      user: { userId: '1', email: 'a@b.com', displayName: 'Alice', accessToken: 'tok' },
    });
    mockedPost.mockResolvedValueOnce({});

    await useAuthStore.getState().logout();

    expect(useAuthStore.getState().user).toBeNull();
  });

  it('clearError: resets error field', () => {
    useAuthStore.setState({ error: 'some error' });
    useAuthStore.getState().clearError();
    expect(useAuthStore.getState().error).toBeNull();
  });
});
