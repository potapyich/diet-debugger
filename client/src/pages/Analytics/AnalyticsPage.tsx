import { useEffect, useState } from 'react';
import api from '../../lib/axios';
import { PatternEventCard, PatternEventEmptyState, type PatternEvent } from '../../components/PatternEventCard/PatternEventCard';
import { useToast } from '../../contexts/ToastContext';

interface ForecastData {
  averageDailyDeltaKcal: number;
  deficitConsistencyPercent: number;
  estimatedWeightChangePer30Days: number;
}

interface MonthlyAssessment {
  insights: string[];
  forecast: {
    estimatedWeightChangePer30Days: number;
    deficitConsistencyPercent: number;
    mainBlocker: string | null;
  };
}

interface MonthlyReport {
  generatedAt: string;
  assessment: MonthlyAssessment;
}

function ForecastCard({ forecast, goalType }: { forecast: ForecastData | null; goalType?: string }) {
  if (!forecast) {
    return (
      <div style={{ padding: 16, background: '#f9fafb', borderRadius: 8, border: '1px solid #e5e7eb' }}>
        <p style={{ margin: 0, color: '#6b7280', fontSize: 14 }}>
          Log at least 7 days to see your forecast.
        </p>
      </div>
    );
  }

  const change = forecast.estimatedWeightChangePer30Days;
  const isCut = goalType === 'Cut';
  const isBulk = goalType === 'Bulk';
  const isGoodDirection = (isCut && change < 0) || (isBulk && change > 0) || (!isCut && !isBulk);
  const changeColor = isGoodDirection ? '#22c55e' : '#ef4444';
  const sign = change > 0 ? '+' : '';

  return (
    <div style={{ padding: 16, background: '#f9fafb', borderRadius: 8, border: '1px solid #e5e7eb' }}>
      <div style={{ fontSize: 28, fontWeight: 700, color: changeColor }}>
        {sign}{change.toFixed(1)} kg/month
      </div>
      <div style={{ fontSize: 13, color: '#6b7280', marginTop: 4 }}>at current pace</div>
      <div style={{ marginTop: 12, fontSize: 14 }}>
        <span style={{ color: '#374151' }}>On-target days: </span>
        <strong>{forecast.deficitConsistencyPercent.toFixed(0)}%</strong>
      </div>
      <div style={{ fontSize: 14, marginTop: 4 }}>
        <span style={{ color: '#374151' }}>Avg daily delta: </span>
        <strong style={{ color: forecast.averageDailyDeltaKcal < 0 ? '#22c55e' : '#ef4444' }}>
          {forecast.averageDailyDeltaKcal > 0 ? '+' : ''}{forecast.averageDailyDeltaKcal.toFixed(0)} kcal
        </strong>
      </div>
    </div>
  );
}

function MonthlyStrategySection() {
  const { showToast } = useToast();
  const [report, setReport] = useState<MonthlyReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [notEnoughData, setNotEnoughData] = useState(false);

  useEffect(() => {
    const month = new Date().toISOString().slice(0, 7); // YYYY-MM
    api.get<MonthlyReport>(`/monthly-report?month=${month}`)
      .then(r => {
        setReport(r.data);
        showToast('InsightsReady', 'Monthly strategy ready!');
      })
      .catch(err => {
        if (err?.response?.status === 404) setNotEnoughData(true);
      })
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div style={{ padding: 16, background: '#f9fafb', borderRadius: 8 }}>
        <div style={{ height: 12, background: '#e5e7eb', borderRadius: 4, marginBottom: 8 }} />
        <div style={{ height: 12, background: '#e5e7eb', borderRadius: 4, width: '80%', marginBottom: 8 }} />
        <div style={{ height: 12, background: '#e5e7eb', borderRadius: 4, width: '60%' }} />
      </div>
    );
  }

  if (notEnoughData) {
    return (
      <p style={{ color: '#6b7280', fontSize: 14 }}>
        Log meals for at least one full week to unlock your monthly strategy.
      </p>
    );
  }

  if (!report) return null;

  return (
    <div>
      <ul style={{ paddingLeft: 20, margin: 0 }}>
        {report.assessment.insights.map((insight, i) => (
          <li key={i} style={{ fontSize: 14, marginBottom: 8, lineHeight: 1.5 }}>{insight}</li>
        ))}
      </ul>
      {report.assessment.forecast.mainBlocker && (
        <div style={{ marginTop: 12, padding: 10, background: '#fef3c7', borderRadius: 6, fontSize: 13 }}>
          <strong>Main blocker:</strong> {report.assessment.forecast.mainBlocker}
        </div>
      )}
      <p style={{ fontSize: 12, color: '#9ca3af', marginTop: 12 }}>
        Generated at: {new Date(report.generatedAt).toLocaleDateString()}
      </p>
    </div>
  );
}

function PatternEventList() {
  const [events, setEvents] = useState<PatternEvent[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get<PatternEvent[]>('/pattern-events?limit=20')
      .then(r => setEvents(r.data))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const handleAcknowledged = (id: string) => {
    setEvents(prev => prev.map(e => e.id === id ? { ...e, acknowledged: true } : e));
  };

  if (loading) return <p style={{ color: '#6b7280', fontSize: 14 }}>Loading...</p>;
  if (events.length === 0) return <PatternEventEmptyState />;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
      {events.map(event => (
        <PatternEventCard key={event.id} event={event} onAcknowledged={handleAcknowledged} />
      ))}
    </div>
  );
}

export function AnalyticsPage() {
  const [forecast, setForecast] = useState<ForecastData | null | 'loading'>('loading');
  const [goalType, setGoalType] = useState<string | undefined>();

  useEffect(() => {
    api.get<{ forecast: ForecastData | null }>('/forecast')
      .then(r => setForecast(r.data.forecast))
      .catch(() => setForecast(null));
    api.get<{ goalType?: string }>('/goals')
      .then(r => setGoalType(r.data.goalType))
      .catch(() => {});
  }, []);

  return (
    <div style={{ padding: '16px', maxWidth: 600, margin: '0 auto' }}>
      <h1 style={{ fontSize: 22, fontWeight: 700, marginBottom: 20 }}>Analytics</h1>

      {/* Forecast Card */}
      <section style={{ marginBottom: 24 }}>
        <h2 style={{ fontSize: 16, fontWeight: 600, marginBottom: 8 }}>Forecast</h2>
        {forecast === 'loading'
          ? <div style={{ height: 80, background: '#f9fafb', borderRadius: 8 }} />
          : <ForecastCard forecast={forecast} goalType={goalType} />
        }
      </section>

      {/* Monthly Strategy */}
      <section style={{ marginBottom: 24 }}>
        <h2 style={{ fontSize: 16, fontWeight: 600, marginBottom: 8 }}>Monthly Strategy</h2>
        <MonthlyStrategySection />
      </section>

      {/* Pattern Breaker Feed */}
      <section>
        <h2 style={{ fontSize: 16, fontWeight: 600, marginBottom: 8 }}>Pattern Breaker</h2>
        <PatternEventList />
      </section>
    </div>
  );
}
