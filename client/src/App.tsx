import { Component, useState } from 'react';
import type { ReactNode } from 'react';
import { BrowserRouter, Routes, Route, Navigate, Link, useLocation } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { LoginPage } from './pages/login-page';
import { RegisterPage } from './pages/register-page';
import { DashboardPage } from './pages/dashboard-page';
import { MarketPage } from './pages/market-page';
import { StockDetailPage } from './pages/stock-detail-page';
import { ProtectedRoute } from './routes/protected-route';
import { GlobalSearch } from './components/search/global-search';

/** Minimal error boundary — catches render errors and shows a fallback UI. */
class ErrorBoundary extends Component<
  { children: ReactNode },
  { hasError: boolean; message: string }
> {
  constructor(props: { children: ReactNode }) {
    super(props);
    this.state = { hasError: false, message: '' };
  }

  static getDerivedStateFromError(error: unknown) {
    return { hasError: true, message: String(error) };
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="flex flex-col items-center justify-center h-screen bg-slate-900 text-slate-200 p-8">
          <p className="text-lg font-semibold mb-2">Something went wrong.</p>
          <pre className="text-xs text-slate-400 max-w-lg whitespace-pre-wrap">{this.state.message}</pre>
          <button
            className="mt-4 px-4 py-2 text-sm bg-blue-600 hover:bg-blue-500 rounded"
            onClick={() => this.setState({ hasError: false, message: '' })}
          >
            Try again
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1 } },
});

const NAV_LINKS = [
  { to: '/market', label: 'Bảng giá' },
  { to: '/dashboard', label: 'Dashboard' },
];

function AppNav() {
  const [menuOpen, setMenuOpen] = useState(false);
  const location = useLocation();

  return (
    <header className="shrink-0 bg-slate-900 border-b border-slate-800">
      {/* Main bar */}
      <div className="flex items-center gap-4 px-4 h-12">
        <Link to="/market" className="text-sm font-semibold text-white hover:text-blue-400 mr-2">
          VnStock
        </Link>

        {/* Desktop nav links */}
        <nav className="hidden sm:flex items-center gap-4">
          {NAV_LINKS.map(({ to, label }) => (
            <Link
              key={to}
              to={to}
              className={`text-sm transition-colors ${
                location.pathname === to
                  ? 'text-white font-medium'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              {label}
            </Link>
          ))}
        </nav>

        {/* Search — shrinks on mobile */}
        <div className="ml-auto flex-1 sm:flex-none sm:w-64 max-w-xs">
          <GlobalSearch />
        </div>

        {/* Hamburger — mobile only */}
        <button
          className="sm:hidden p-1.5 text-slate-400 hover:text-white"
          onClick={() => setMenuOpen((v) => !v)}
          aria-label="Toggle menu"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            {menuOpen
              ? <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              : <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            }
          </svg>
        </button>
      </div>

      {/* Mobile dropdown menu */}
      {menuOpen && (
        <nav className="sm:hidden border-t border-slate-800 px-4 py-2 flex flex-col gap-1">
          {NAV_LINKS.map(({ to, label }) => (
            <Link
              key={to}
              to={to}
              onClick={() => setMenuOpen(false)}
              className={`py-2 text-sm transition-colors ${
                location.pathname === to
                  ? 'text-white font-medium'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              {label}
            </Link>
          ))}
        </nav>
      )}
    </header>
  );
}

export function App() {
  return (
    <ErrorBoundary>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          {/* Public auth routes — no nav */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Protected app routes — with nav */}
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <div className="flex flex-col h-screen">
                  <AppNav />
                  <main className="flex-1 overflow-hidden">
                    <Routes>
                      <Route path="/dashboard" element={<DashboardPage />} />
                      <Route path="/market" element={<MarketPage />} />
                      <Route path="/stocks/:symbol" element={<StockDetailPage />} />
                      <Route path="*" element={<Navigate to="/market" replace />} />
                    </Routes>
                  </main>
                </div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
    </ErrorBoundary>
  );
}
