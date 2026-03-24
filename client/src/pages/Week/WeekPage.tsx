import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import api from '../../lib/axios';
import { CalendarGrid } from '../../components/CalendarGrid/CalendarGrid';
import { useToast } from '../../contexts/ToastContext';
import { SkeletonBlock } from '../../components/Skeleton/Skeleton';
import { ErrorBanner } from '../../components/ErrorBanner/ErrorBanner';
import { EmptyState } from '../../components/EmptyState/EmptyState';

interface WeeklyReport {
  patterns: string;
  calendarDayStatuses: string;
  generatedAt: string;
  isStale: boolean;
}

function getMondayDate(date: Date): string {
  const d = new Date(date);
  const day = d.getUTCDay();
  const diff = day === 0 ? -6 : 1 - day;
  d.setUTCDate(d.getUTCDate() + diff);
  return d.toISOString().split('T')[0];
}

export function WeekPage() {
  const [searchParams] = useSearchParams();
  const { showToast } = useToast();
  const [weekStart, setWeekStart] = useState<string>(() =>
    searchParams.get('weekStart') || getMondayDate(new Date()));
  const [report, setReport] = useState<WeeklyReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);

  useEffect(() => {
    loadReport();
  }, [weekStart]);

  const loadReport = async () => {
    setLoading(true);
    setError(false);
    try {
      const { data } = await api.get(`/weekly-report?weekStart=${weekStart}`);
      const isNew = report == null;
      setReport(data);
      if (isNew && !data.isStale) {
        showToast('InsightsReady', 'Weekly report ready!');
      }
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  };

  const navigate = (direction: -1 | 1) => {
    const d = new Date(weekStart + 'T00:00:00Z');
    d.setUTCDate(d.getUTCDate() + 7 * direction);
    setWeekStart(d.toISOString().split('T')[0]);
  };

  const parseDate = (d: Date) => d.toLocaleString('en', { month: 'short', day: 'numeric' });
  const weekEndDate = new Date(weekStart + 'T00:00:00Z');
  weekEndDate.setUTCDate(weekEndDate.getUTCDate() + 6);

  const patterns: string[] = (() => {
    if (!report?.patterns) return [];
    try { return JSON.parse(report.patterns); } catch { return []; }
  })();

  const dayStatuses = (() => {
    if (!report?.calendarDayStatuses) return [];
    try { return JSON.parse(report.calendarDayStatuses); } catch { return []; }
  })();

  return (
    <div className="week-page" style={{ padding: 16, maxWidth: 480, margin: '0 auto' }}>
      {/* Week navigation */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 16, marginBottom: 24 }}>
        <button onClick={() => navigate(-1)}>‹</button>
        <span>Week of {parseDate(new Date(weekStart + 'T00:00:00Z'))} – {parseDate(weekEndDate)}</span>
        <button onClick={() => navigate(1)}>›</button>
      </div>

      {/* Calendar grid */}
      <CalendarGrid weekStart={weekStart} dayStatuses={dayStatuses} />

      {/* Patterns list */}
      <div className="patterns-list" style={{ marginTop: 24 }}>
        <h3>Weekly Patterns</h3>
        {loading ? (
          <SkeletonBlock lines={4} />
        ) : error ? (
          <ErrorBanner message="Could not load weekly report." onRetry={loadReport} />
        ) : patterns.length === 0 ? (
          <EmptyState
            icon="📊"
            heading="Not enough data yet"
            subtext="Log meals for at least 3 days this week to see patterns."
          />
        ) : (
          <ul>
            {patterns.map((p, i) => <li key={i}>{p}</li>)}
          </ul>
        )}
      </div>
    </div>
  );
}
