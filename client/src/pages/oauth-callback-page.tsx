import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '../stores/auth-store';

export function OAuthCallbackPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { setAuthFromToken, error } = useAuthStore();

  useEffect(() => {
    const token = params.get('token');
    const errorMsg = params.get('error');

    if (errorMsg) {
      useAuthStore.setState({ error: errorMsg, isLoading: false });
      return;
    }

    if (token) {
      // Clear token from URL immediately for security
      window.history.replaceState({}, '', '/auth/callback');
      setAuthFromToken(token).then(() => {
        if (!useAuthStore.getState().error) navigate('/dashboard');
      });
    } else {
      navigate('/login');
    }

    // Clear OAuth error state on unmount to avoid stale errors on subsequent navigation
    return () => { useAuthStore.setState({ error: null }); };
  }, [navigate, setAuthFromToken, params]);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="max-w-md w-full p-8 bg-white rounded-lg shadow text-center">
          <p className="text-red-600 mb-4">{error}</p>
          <a href="/login" className="text-blue-600 hover:underline">
            Back to login
          </a>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <p className="text-gray-500">Signing you in...</p>
    </div>
  );
}
