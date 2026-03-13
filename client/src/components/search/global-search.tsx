import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Command } from 'cmdk';
import { fetchStocks } from '../../services/market-api';
import { SearchResultItem } from './search-result-item';
import { useDebounce } from '../../hooks/use-debounce';
import type { StockInfo } from '../../types/market';

/**
 * Global search bar with keyboard navigation via cmdk.
 * Renders an inline command palette dropdown.
 */
export function GlobalSearch() {
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const debouncedQuery = useDebounce(query, 300);

  const { data: results = [] } = useQuery<StockInfo[]>({
    queryKey: ['stock-search', debouncedQuery],
    queryFn: () => fetchStocks({ q: debouncedQuery }),
    enabled: debouncedQuery.length >= 1,
    staleTime: 30_000,
  });

  function handleSelect(symbol: string) {
    setOpen(false);
    setQuery('');
    navigate(`/stocks/${symbol}`);
  }

  return (
    <div className="relative w-64">
      <Command
        className="relative"
        shouldFilter={false} // server-side filtering
        onKeyDown={(e) => {
          if (e.key === 'Escape') {
            setOpen(false);
            setQuery('');
          }
        }}
      >
        <div className="flex items-center px-3 rounded-lg bg-slate-800 border border-slate-700 focus-within:border-blue-500 transition-colors">
          <svg className="w-4 h-4 text-slate-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <Command.Input
            value={query}
            onValueChange={(v) => {
              setQuery(v);
              setOpen(v.length > 0);
            }}
            onBlur={() => setTimeout(() => setOpen(false), 150)}
            onFocus={() => { if (query.length > 0) setOpen(true); }}
            placeholder="Tìm mã cổ phiếu…"
            className="w-full bg-transparent px-2 py-2 text-sm text-slate-100
                       placeholder:text-slate-500 outline-none"
          />
        </div>

        {open && results.length > 0 && (
          <Command.List
            className="absolute z-50 top-full mt-1 w-full rounded-lg border border-slate-700
                       bg-slate-900 shadow-xl overflow-hidden max-h-72 overflow-y-auto"
          >
            <Command.Empty className="px-4 py-3 text-sm text-slate-500">
              Không tìm thấy kết quả.
            </Command.Empty>

            {results.map((stock) => (
              <Command.Item
                key={stock.symbol}
                value={stock.symbol}
                onSelect={() => handleSelect(stock.symbol)}
                className="cursor-pointer hover:bg-slate-800 aria-selected:bg-slate-800
                           transition-colors outline-none"
              >
                <SearchResultItem stock={stock} />
              </Command.Item>
            ))}
          </Command.List>
        )}
      </Command>
    </div>
  );
}
