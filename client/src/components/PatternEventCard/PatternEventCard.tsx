import api from '../../lib/axios';
import { EmptyState } from '../EmptyState/EmptyState';

export interface PatternEvent {
  id: string;
  patternKey: string;
  type: 'Positive' | 'Warning';
  message: string;
  triggeredAt: string;
  acknowledged: boolean;
}

interface PatternEventCardProps {
  event: PatternEvent;
  onAcknowledged: (id: string) => void;
}

export function PatternEventEmptyState() {
  return (
    <EmptyState
      icon="✨"
      heading="No patterns yet"
      subtext="Keep logging meals to unlock behavioral insights."
    />
  );
}

export function PatternEventCard({ event, onAcknowledged }: PatternEventCardProps) {
  const icon = event.type === 'Positive' ? '✓' : '⚠';
  const color = event.type === 'Positive' ? '#22c55e' : '#f59e0b';

  const acknowledge = async () => {
    try {
      await api.post(`/pattern-events/${event.id}/acknowledge`);
      onAcknowledged(event.id);
    } catch {
      // silently fail — user can retry
    }
  };

  const date = new Date(event.triggeredAt);
  const formatted = date.toLocaleDateString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
  });

  return (
    <div style={{
      display: 'flex',
      alignItems: 'flex-start',
      gap: 12,
      padding: '12px 16px',
      borderRadius: 8,
      background: '#f9fafb',
      border: `1px solid ${color}33`,
    }}>
      <span style={{ fontSize: 20, color, lineHeight: 1.4 }}>{icon}</span>
      <div style={{ flex: 1 }}>
        <p style={{ margin: 0, fontSize: 14 }}>{event.message}</p>
        <p style={{ margin: '4px 0 0', fontSize: 12, color: '#6b7280' }}>{formatted}</p>
      </div>
      {!event.acknowledged && (
        <button
          onClick={acknowledge}
          style={{
            fontSize: 12,
            padding: '4px 10px',
            borderRadius: 6,
            border: '1px solid #d1d5db',
            background: '#fff',
            cursor: 'pointer',
          }}
        >
          Got it
        </button>
      )}
    </div>
  );
}
