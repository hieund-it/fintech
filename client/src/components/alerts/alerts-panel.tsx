import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fetchAlerts, createAlert, deleteAlert, deactivateAlert } from '../../services/alert-api';
import type { CreateAlertRequest } from '../../types/alert';

export function AlertsPanel() {
  const qc = useQueryClient();
  const [showForm, setShowForm] = useState(false);

  const { data: alerts = [], isLoading } = useQuery({
    queryKey: ['alerts'],
    queryFn: fetchAlerts,
  });

  const createMutation = useMutation({
    mutationFn: (req: CreateAlertRequest) => createAlert(req),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['alerts'] });
      setShowForm(false);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteAlert(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['alerts'] }),
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateAlert(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['alerts'] }),
  });

  if (isLoading) return <div className="p-4 text-sm text-slate-400">Đang tải cảnh báo…</div>;

  const active = alerts.filter((a) => a.isActive);
  const triggered = alerts.filter((a) => !a.isActive && a.triggeredAt);

  return (
    <div className="bg-slate-900 rounded-lg border border-slate-700 overflow-hidden">
      <div className="px-4 py-3 border-b border-slate-700 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-white">Cảnh báo giá</h3>
        <button
          onClick={() => setShowForm((v) => !v)}
          className="text-xs text-blue-400 hover:text-blue-300 transition-colors"
        >
          {showForm ? 'Hủy' : '+ Tạo cảnh báo'}
        </button>
      </div>

      {showForm && (
        <div className="px-4 py-3 border-b border-slate-700">
          <CreateAlertForm
            onSubmit={(req) => createMutation.mutate(req)}
            isPending={createMutation.isPending}
          />
        </div>
      )}

      {/* Active alerts */}
      {active.length > 0 && (
        <div>
          <p className="px-4 py-2 text-xs text-slate-500 uppercase tracking-wide border-b border-slate-800">
            Đang theo dõi ({active.length})
          </p>
          <ul>
            {active.map((alert) => (
              <li key={alert.id}
                className="flex items-center justify-between px-4 py-2.5 border-b border-slate-800 hover:bg-slate-800/30 group">
                <div>
                  <span className="text-sm font-medium text-white">{alert.symbol}</span>
                  <span className={`ml-2 text-xs px-1.5 py-0.5 rounded font-medium
                    ${alert.direction === 'ABOVE' ? 'bg-green-900/50 text-green-400' : 'bg-red-900/50 text-red-400'}`}>
                    {alert.direction === 'ABOVE' ? '↑ Trên' : '↓ Dưới'}
                  </span>
                  <span className="ml-2 text-xs text-slate-400">
                    {alert.threshold.toLocaleString('vi-VN')} ₫
                  </span>
                </div>
                <div className="flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                  <button
                    onClick={() => deactivateMutation.mutate(alert.id)}
                    className="text-xs text-slate-500 hover:text-yellow-400 transition-colors"
                    title="Tắt cảnh báo"
                  >
                    Tắt
                  </button>
                  <button
                    onClick={() => deleteMutation.mutate(alert.id)}
                    className="text-xs text-slate-500 hover:text-red-400 transition-colors"
                    title="Xóa"
                  >
                    ✕
                  </button>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Triggered alerts */}
      {triggered.length > 0 && (
        <div>
          <p className="px-4 py-2 text-xs text-slate-500 uppercase tracking-wide border-b border-slate-800">
            Đã kích hoạt ({triggered.length})
          </p>
          <ul>
            {triggered.slice(0, 5).map((alert) => (
              <li key={alert.id}
                className="flex items-center justify-between px-4 py-2.5 border-b border-slate-800 opacity-60 hover:bg-slate-800/20 group">
                <div>
                  <span className="text-sm text-slate-400 line-through">{alert.symbol}</span>
                  <span className="ml-2 text-xs text-slate-500">
                    {alert.direction} {alert.threshold.toLocaleString('vi-VN')} ₫
                  </span>
                  {alert.triggeredAt && (
                    <span className="ml-2 text-xs text-slate-600">
                      {new Date(alert.triggeredAt).toLocaleDateString('vi-VN')}
                    </span>
                  )}
                </div>
                <button
                  onClick={() => deleteMutation.mutate(alert.id)}
                  className="text-xs text-slate-600 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-all"
                >
                  ✕
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}

      {alerts.length === 0 && (
        <p className="px-4 py-8 text-center text-slate-500 text-sm">
          Chưa có cảnh báo nào. Tạo cảnh báo để nhận email khi giá vượt ngưỡng.
        </p>
      )}
    </div>
  );
}

// --- Create Alert Form ---

interface CreateAlertFormProps {
  onSubmit: (req: CreateAlertRequest) => void;
  isPending: boolean;
}

function CreateAlertForm({ onSubmit, isPending }: CreateAlertFormProps) {
  const [form, setForm] = useState<CreateAlertRequest>({
    symbol: '',
    direction: 'ABOVE',
    threshold: 0,
  });

  const set = <K extends keyof CreateAlertRequest>(k: K, v: CreateAlertRequest[K]) =>
    setForm((f) => ({ ...f, [k]: v }));

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit(form);
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-wrap gap-2 items-end">
      <input
        required value={form.symbol} onChange={(e) => set('symbol', e.target.value.toUpperCase())}
        placeholder="Mã CK" maxLength={10}
        className="bg-slate-800 text-white text-xs px-2 py-1.5 rounded border border-slate-600
                   focus:outline-none focus:border-blue-500 placeholder-slate-500 w-24"
      />
      <select
        value={form.direction} onChange={(e) => set('direction', e.target.value as 'ABOVE' | 'BELOW')}
        className="bg-slate-800 text-white text-xs px-2 py-1.5 rounded border border-slate-600 focus:outline-none focus:border-blue-500"
      >
        <option value="ABOVE">↑ Trên</option>
        <option value="BELOW">↓ Dưới</option>
      </select>
      <input
        required type="number" min={1} value={form.threshold || ''} onChange={(e) => set('threshold', +e.target.value)}
        placeholder="Ngưỡng (VND)"
        className="bg-slate-800 text-white text-xs px-2 py-1.5 rounded border border-slate-600
                   focus:outline-none focus:border-blue-500 placeholder-slate-500 w-36"
      />
      <button
        type="submit" disabled={!form.symbol || !form.threshold || isPending}
        className="text-xs px-3 py-1.5 bg-blue-600 hover:bg-blue-500 disabled:opacity-50 text-white rounded transition-colors"
      >
        Tạo
      </button>
    </form>
  );
}
