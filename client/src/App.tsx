import { BrowserRouter, Routes, Route, Navigate, Link } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { LoginPage } from './pages/login-page';
import { RegisterPage } from './pages/register-page';
import { DashboardPage } from './pages/dashboard-page';
import { MarketPage } from './pages/market-page';
import { StockDetailPage } from './pages/stock-detail-page';
import { ProtectedRoute } from './routes/protected-route';
import { GlobalSearch } from './components/search/global-search';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1 } },
});

function AppNav() {
  return (
    <header className="flex items-center gap-6 px-6 h-12 bg-slate-900 border-b border-slate-800 shrink-0">
      <Link to="/market" className="text-sm font-semibold text-white hover:text-blue-400">
        VnStock
      </Link>
      <Link to="/market" className="text-sm text-slate-400 hover:text-slate-200">
        Bảng giá
      </Link>
      <Link to="/dashboard" className="text-sm text-slate-400 hover:text-slate-200">
        Dashboard
      </Link>
      <div className="ml-auto">
        <GlobalSearch />
      </div>
    </header>
  );
}

export function App() {
  return (
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
  );
}
