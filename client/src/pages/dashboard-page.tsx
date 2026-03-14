import { WatchlistPanel } from '../components/watchlist/watchlist-panel';
import { PortfolioPanel } from '../components/portfolio/portfolio-panel';
import { AlertsPanel } from '../components/alerts/alerts-panel';

/** Dashboard: Portfolio P&L + Watchlist + Price Alerts in a responsive 3-column grid. */
export function DashboardPage() {
  return (
    <div className="h-full overflow-y-auto bg-slate-950 p-6">
      <div className="max-w-7xl mx-auto space-y-6">
        <h2 className="text-lg font-semibold text-white">Dashboard</h2>

        <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
          {/* Portfolio takes 2 columns on wide screens */}
          <div className="xl:col-span-2">
            <PortfolioPanel />
          </div>

          {/* Watchlist */}
          <div>
            <WatchlistPanel />
          </div>
        </div>

        {/* Alerts full width below */}
        <AlertsPanel />
      </div>
    </div>
  );
}
