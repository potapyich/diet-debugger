import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';

export type ToastType = 'InsightsReady' | 'PatternAlert' | 'StreakAlert' | 'Error';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
  durationMs: number;
  visible: boolean;
}

interface ToastContextValue {
  toasts: Toast[];
  showToast: (type: ToastType, message: string, durationMs?: number) => void;
  dismissToast: (id: string) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

const DEFAULT_DURATION = 4000;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const dismissToast = useCallback((id: string) => {
    setToasts(prev => prev.map(t => t.id === id ? { ...t, visible: false } : t));
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 300);
  }, []);

  const showToast = useCallback((type: ToastType, message: string, durationMs = DEFAULT_DURATION) => {
    const id = crypto.randomUUID();
    const toast: Toast = { id, type, message, durationMs, visible: true };
    setToasts(prev => [...prev, toast]);
    setTimeout(() => dismissToast(id), durationMs);
  }, [dismissToast]);

  return (
    <ToastContext.Provider value={{ toasts, showToast, dismissToast }}>
      {children}
      <ToastContainer toasts={toasts} onDismiss={dismissToast} />
    </ToastContext.Provider>
  );
}

function ToastContainer({ toasts, onDismiss }: { toasts: Toast[]; onDismiss: (id: string) => void }) {
  if (toasts.length === 0) return null;
  return (
    <div style={{
      position: 'fixed', top: 16, right: 16, zIndex: 9999,
      display: 'flex', flexDirection: 'column', gap: 8
    }}>
      {toasts.map(toast => (
        <ToastItem key={toast.id} toast={toast} onDismiss={onDismiss} />
      ))}
    </div>
  );
}

function ToastItem({ toast, onDismiss }: { toast: Toast; onDismiss: (id: string) => void }) {
  const colorMap: Record<ToastType, string> = {
    InsightsReady: '#3b82f6',
    PatternAlert: '#f59e0b',
    StreakAlert: '#22c55e',
    Error: '#ef4444',
  };

  return (
    <div style={{
      background: colorMap[toast.type],
      color: '#fff',
      padding: '12px 16px',
      borderRadius: 8,
      minWidth: 260,
      display: 'flex',
      justifyContent: 'space-between',
      alignItems: 'center',
      opacity: toast.visible ? 1 : 0,
      transform: toast.visible ? 'translateX(0)' : 'translateX(100%)',
      transition: 'opacity 0.3s, transform 0.3s',
    }}>
      <span>{toast.message}</span>
      <button
        onClick={() => onDismiss(toast.id)}
        style={{ background: 'none', border: 'none', color: '#fff', cursor: 'pointer', marginLeft: 12 }}
      >
        ✕
      </button>
    </div>
  );
}

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within ToastProvider');
  return ctx;
}
