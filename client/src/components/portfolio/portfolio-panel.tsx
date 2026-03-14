import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  fetchPortfolios,
  createPortfolio,
  fetchPnL,
  addTransaction,
} from '../../services/portfolio-api';
import type { CreateTransactionRequest } from '../../types/portfolio';

/** Portfolio panel: list portfolios, view P&L, add/remove transactions. */
export function PortfolioPanel() {
  const qc = useQueryClient();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [newName, setNewName] = useState('');
  const [showTxnForm, setShowTxnForm] = useState(false);

  const { data: portfolios = [] } = useQuery({
    queryKey: ['portfolios'],
    queryFn: fetchPortfolios,
  });

  const { data: pnl, isLoading: pnlLoading } = useQuery({
    queryKey: ['pnl', selectedId],
    queryFn: () => fetchPnL(selectedId!),
    enabled: !!selectedId,
  });

  const createMutation = useMutation({
    mutationFn: (name: string) => createPortfolio(name),
    onSuccess: (p) => {
      void qc.invalidateQueries({ queryKey: ['portfolios'] });
      setSelectedId(p.id);
      setNewName('');
    },
  });

  const addTxnMutation = useMutation({
    mutationFn: (req: CreateTransactionRequest) => addTransaction(selectedId!, req),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['pnl', selectedId] });
      setShowTxnForm(false);
    },
  });


  const handleCreatePortfolio = (e: React.FormEvent) => {
    e.preventDefault();
    if (newName.trim()) createMutation.mutate(newName.trim());
  };

  return (
    <div className="bg-slate-900 rounded-lg border border-slate-700 overflow-hidden">
      <div className="px-4 py-3 border-b border-slate-700">
        <h3 className="text-sm font-semibold text-white">Portfolio</h3>
      </div>

      {/* Portfolio selector + create */}
      <div className="px-4 py-3 border-b border-slate-700 flex flex-wrap gap-2 items-center">
        {portfolios.map((p) => (
          <button
            key={p.id}
            onClick={() => setSelectedId(p.id)}
            className={`text-xs px-3 py-1.5 rounded border transition-colors ${
              selectedId === p.id
                ? 'bg-blue-600 border-blue-500 text-white'
                : 'bg-slate-800 border-slate-600 text-slate-300 hover:border-slate-400'
            }`}
          >
            {p.name}
          </button>
        ))}
        <form onSubmit={handleCreatePortfolio} className="flex gap-1.5">
          <input
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="Tên portfolio mới"
            className="bg-slate-800 text-white text-xs px-2 py-1.5 rounded border border-slate-600
                       focus:outline-none focus:border-blue-500 placeholder-slate-500 w-36"
          />
          <button
            type="submit"
            disabled={!newName.trim()}
            className="text-xs px-2 py-1.5 bg-slate-700 hover:bg-slate-600 disabled:opacity-40
                       text-white rounded border border-slate-600 transition-colors"
          >
            +
          </button>
        </form>
      </div>

      {!selectedId ? (
        <p className="px-4 py-8 text-center text-slate-500 text-sm">
          Chọn hoặc tạo portfolio để xem P&amp;L.
        </p>
      ) : pnlLoading ? (
        <div className="px-4 py-6 text-center text-slate-400 text-sm">Đang tính P&amp;L…</div>
      ) : pnl ? (
        <div>
          {/* Summary */}
          <div className="px-4 py-3 border-b border-slate-700 grid grid-cols-2 gap-4">
            <div>
              <p className="text-xs text-slate-500">Lãi/Lỗ thực hiện</p>
              <p className={`text-sm font-semibold ${pnl.totalRealizedPnL >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                {formatVnd(pnl.totalRealizedPnL)}
              </p>
            </div>
            <div>
              <p className="text-xs text-slate-500">Lãi/Lỗ chưa thực hiện</p>
              <p className={`text-sm font-semibold ${pnl.totalUnrealizedPnL >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                {formatVnd(pnl.totalUnrealizedPnL)}
              </p>
            </div>
          </div>

          {/* Positions table */}
          {pnl.positions.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr className="text-slate-500 border-b border-slate-700">
                    {['Mã', 'SL', 'Giá TB', 'Giá hiện tại', 'L/L chưa TH'].map((h) => (
                      <th key={h} className="px-4 py-2 text-left font-medium">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {pnl.positions.map((pos) => (
                    <tr key={pos.symbol} className="border-b border-slate-800 hover:bg-slate-800/30">
                      <td className="px-4 py-2 font-medium text-white">{pos.symbol}</td>
                      <td className="px-4 py-2 text-slate-300">{pos.quantity.toLocaleString('vi-VN')}</td>
                      <td className="px-4 py-2 text-slate-300">{formatVnd(pos.avgCost)}</td>
                      <td className="px-4 py-2 text-slate-300">{pos.currentPrice > 0 ? formatVnd(pos.currentPrice) : '—'}</td>
                      <td className={`px-4 py-2 font-medium ${pos.unrealizedPnL >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                        {formatVnd(pos.unrealizedPnL)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Add transaction */}
          <div className="px-4 py-3 border-t border-slate-700">
            {showTxnForm ? (
              <TransactionForm
                onSubmit={(req) => addTxnMutation.mutate(req)}
                onCancel={() => setShowTxnForm(false)}
                isPending={addTxnMutation.isPending}
              />
            ) : (
              <button
                onClick={() => setShowTxnForm(true)}
                className="text-xs text-blue-400 hover:text-blue-300 transition-colors"
              >
                + Thêm giao dịch
              </button>
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}

// --- Transaction form ---

interface TransactionFormProps {
  onSubmit: (req: CreateTransactionRequest) => void;
  onCancel: () => void;
  isPending: boolean;
}

function TransactionForm({ onSubmit, onCancel, isPending }: TransactionFormProps) {
  const [form, setForm] = useState<CreateTransactionRequest>({
    symbol: '',
    type: 'BUY',
    quantity: 0,
    price: 0,
    fee: 0,
    transactedAt: new Date().toISOString().slice(0, 10),
  });

  const set = <K extends keyof CreateTransactionRequest>(k: K, v: CreateTransactionRequest[K]) =>
    setForm((f) => ({ ...f, [k]: v }));

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ ...form, transactedAt: new Date(form.transactedAt).toISOString() });
  };

  return (
    <form onSubmit={handleSubmit} className="grid grid-cols-2 gap-2">
      <input
        required value={form.symbol} onChange={(e) => set('symbol', e.target.value.toUpperCase())}
        placeholder="Mã CK" className={inputCls}
      />
      <select value={form.type} onChange={(e) => set('type', e.target.value as 'BUY' | 'SELL')} className={inputCls}>
        <option value="BUY">Mua</option>
        <option value="SELL">Bán</option>
      </select>
      <input
        required type="number" min={1} value={form.quantity || ''} onChange={(e) => set('quantity', +e.target.value)}
        placeholder="Số lượng" className={inputCls}
      />
      <input
        required type="number" min={0} value={form.price || ''} onChange={(e) => set('price', +e.target.value)}
        placeholder="Giá (VND)" className={inputCls}
      />
      <input
        type="number" min={0} value={form.fee || ''} onChange={(e) => set('fee', +e.target.value)}
        placeholder="Phí (VND)" className={inputCls}
      />
      <input
        required type="date" value={form.transactedAt} onChange={(e) => set('transactedAt', e.target.value)}
        className={inputCls}
      />
      <div className="col-span-2 flex gap-2 justify-end">
        <button type="button" onClick={onCancel} className="text-xs text-slate-400 hover:text-slate-200 px-3 py-1.5">
          Hủy
        </button>
        <button type="submit" disabled={isPending}
          className="text-xs px-3 py-1.5 bg-blue-600 hover:bg-blue-500 disabled:opacity-50 text-white rounded transition-colors">
          Lưu
        </button>
      </div>
    </form>
  );
}

const inputCls = 'bg-slate-800 text-white text-xs px-2 py-1.5 rounded border border-slate-600 focus:outline-none focus:border-blue-500 placeholder-slate-500 w-full';

function formatVnd(amount: number): string {
  return amount.toLocaleString('vi-VN') + ' ₫';
}
