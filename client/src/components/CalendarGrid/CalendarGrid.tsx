import { useNavigate } from 'react-router-dom';

interface DayStatus {
  date: string;
  status: 'green' | 'yellow' | 'red';
}

interface CalendarGridProps {
  weekStart: string;
  dayStatuses: DayStatus[];
}

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const COLOR = { green: '#22c55e', yellow: '#f59e0b', red: '#ef4444' };

export function CalendarGrid({ weekStart, dayStatuses }: CalendarGridProps) {
  const navigate = useNavigate();
  const statusMap = Object.fromEntries(dayStatuses.map(d => [d.date, d.status]));

  const days = Array.from({ length: 7 }, (_, i) => {
    const d = new Date(weekStart + 'T00:00:00Z');
    d.setUTCDate(d.getUTCDate() + i);
    return d.toISOString().split('T')[0];
  });

  return (
    <div className="calendar-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: 4 }}>
      {DAYS.map((label, i) => (
        <div key={i} style={{ textAlign: 'center' }}>
          <div style={{ fontSize: 12, color: '#6b7280', marginBottom: 4 }}>{label}</div>
          <button
            onClick={() => navigate(`/day?date=${days[i]}`)}
            style={{
              width: '100%',
              aspectRatio: '1',
              borderRadius: 8,
              border: 'none',
              background: statusMap[days[i]] ? COLOR[statusMap[days[i]]] : '#e5e7eb',
              cursor: 'pointer',
              fontSize: 11,
              color: statusMap[days[i]] ? '#fff' : '#9ca3af',
            }}
          >
            {new Date(days[i] + 'T00:00:00Z').getUTCDate()}
          </button>
        </div>
      ))}
    </div>
  );
}
