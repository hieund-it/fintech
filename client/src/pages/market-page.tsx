import { PriceBoard } from '../components/price-board/price-board';

/**
 * /market — main price board page.
 * GlobalSearch (Task 09) will be added to the app-level header, not here.
 */
export function MarketPage() {
  return (
    <div className="flex flex-col h-screen bg-slate-900">
      <PriceBoard />
    </div>
  );
}
