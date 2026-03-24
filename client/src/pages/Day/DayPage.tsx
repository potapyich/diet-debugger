import { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import api from '../../lib/axios';
import { CalorieRing } from '../../components/CalorieRing';
import { AddMealSheet } from '../../components/AddMeal/AddMealSheet';
import { useToast } from '../../contexts/ToastContext';
import { SkeletonBlock } from '../../components/Skeleton/Skeleton';
import { ErrorBanner } from '../../components/ErrorBanner/ErrorBanner';
import { EmptyState } from '../../components/EmptyState/EmptyState';
import { ProfilePrompt } from '../../components/ProfilePrompt/ProfilePrompt';

interface Meal {
  id: string;
  calories: number;
  proteinG: number;
  fatG: number;
  carbsG: number;
  loggedAt: string;
  portionEstimate: string;
}

interface DailySummaryData {
  totalCalories: number;
  totalProteinG: number;
  totalFatG: number;
  totalCarbsG: number;
  insights: string;
  isStale: boolean;
}

interface ProfileData {
  profileCompletedAt?: string | null;
}

export function DayPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { showToast } = useToast();
  const [date, setDate] = useState<DateOnly>(() => {
    const param = searchParams.get('date');
    return param ? DateOnly.parse(param) : DateOnly.today();
  });
  const [meals, setMeals] = useState<Meal[]>([]);
  const [summary, setSummary] = useState<DailySummaryData | null>(null);
  const [profile, setProfile] = useState<ProfileData | null>(null);
  const [summaryLoading, setSummaryLoading] = useState(false);
  const [showSheet, setShowSheet] = useState(false);
  const [summaryError, setSummaryError] = useState(false);
  const [mealsError, setMealsError] = useState(false);
  const [showProfilePrompt, setShowProfilePrompt] = useState(() => {
    return !localStorage.getItem('hasSeenProfilePrompt');
  });

  const dateStr = date.toString();

  useEffect(() => {
    loadMeals();
    loadSummary();
    loadProfile();
  }, [dateStr]);

  const loadMeals = async () => {
    setMealsError(false);
    try {
      const { data } = await api.get(`/meals?date=${dateStr}`);
      setMeals(data);
    } catch {
      setMealsError(true);
    }
  };

  const loadSummary = async () => {
    setSummaryLoading(true);
    setSummaryError(false);
    try {
      const { data } = await api.get(`/daily-summary?date=${dateStr}`);
      setSummary(data);
      if (data.insights && !data.isStale) {
        showToast('InsightsReady', 'Daily insights ready!');
      }
    } catch {
      setSummaryError(true);
    } finally {
      setSummaryLoading(false);
    }
  };

  const loadProfile = async () => {
    try {
      const { data } = await api.get('/profile');
      setProfile(data);
    } catch {
      // profile may not exist yet
    }
  };

  const deleteMeal = async (id: string, _mealDate: string) => {
    try {
      await api.delete(`/meals/${id}`);
      setMeals(prev => prev.filter(m => m.id !== id));
    } catch {
      showToast('Error', 'Failed to delete meal.');
    }
  };

  const targetCalories = 2000; // TODO: use profile goal when available
  const consumedCalories = meals.reduce((sum, m) => sum + m.calories, 0);

  const insightsList: string[] = (() => {
    if (!summary?.insights) return [];
    try { return JSON.parse(summary.insights); } catch { return []; }
  })();

  const dismissProfilePrompt = () => {
    localStorage.setItem('hasSeenProfilePrompt', '1');
    setShowProfilePrompt(false);
  };

  return (
    <div className="day-page">
      {showProfilePrompt && !profile?.profileCompletedAt && meals.length === 0 && (
        <ProfilePrompt onDismiss={dismissProfilePrompt} />
      )}

      {/* Date navigation */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 16, padding: 16 }}>
        <button onClick={() => setDate(date.addDays(-1))}>‹</button>
        <strong>{dateStr}</strong>
        <button onClick={() => setDate(date.addDays(1))}>›</button>
      </div>

      {/* Profile nudge */}
      {profile && !profile.profileCompletedAt && (
        <div className="profile-nudge" style={{ background: '#fef3c7', padding: 12, margin: '0 16px', borderRadius: 8 }}>
          <span>Complete your profile for personalized insights.</span>
          <button onClick={() => navigate('/profile')}>Set up profile</button>
        </div>
      )}

      {/* Calorie ring */}
      <CalorieRing consumed={consumedCalories} target={targetCalories} />

      {/* Macro bars */}
      <div className="macro-bars" style={{ padding: '0 16px' }}>
        {[
          { label: 'Protein', val: meals.reduce((s, m) => s + m.proteinG, 0), target: 50 },
          { label: 'Fat', val: meals.reduce((s, m) => s + m.fatG, 0), target: 65 },
          { label: 'Carbs', val: meals.reduce((s, m) => s + m.carbsG, 0), target: 260 },
        ].map(({ label, val, target }) => (
          <div key={label} style={{ marginBottom: 8 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span>{label}</span>
              <span>{val.toFixed(0)}g / {target}g</span>
            </div>
            <div style={{ background: '#e5e7eb', height: 6, borderRadius: 3 }}>
              <div style={{
                background: '#3b82f6',
                height: 6,
                borderRadius: 3,
                width: `${Math.min(val / target * 100, 100)}%`,
                transition: 'width 0.3s'
              }} />
            </div>
          </div>
        ))}
      </div>

      {/* Meal list */}
      <div className="meal-list" style={{ padding: 16 }}>
        <h3>Meals</h3>
        {mealsError ? (
          <ErrorBanner message="Could not load meals." onRetry={loadMeals} />
        ) : meals.length === 0 ? (
          <EmptyState
            icon="🍽️"
            heading="No meals yet"
            subtext="Tap + to log your first meal of the day."
            ctaLabel="Add meal"
            onCta={() => setShowSheet(true)}
          />
        ) : (
          meals.map(meal => (
            <div key={meal.id} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, marginBottom: 8, display: 'flex', justifyContent: 'space-between' }}>
              <div>
                <div>{Math.round(meal.calories)} kcal</div>
                <div style={{ fontSize: 12, color: '#6b7280' }}>
                  P: {meal.proteinG.toFixed(0)}g · F: {meal.fatG.toFixed(0)}g · C: {meal.carbsG.toFixed(0)}g
                </div>
              </div>
              <div>
                <button onClick={() => navigate(`/meals/${meal.id}/edit`)}>Edit</button>
                <button onClick={() => deleteMeal(meal.id, meal.loggedAt)}>Delete</button>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Daily insights */}
      <div className="daily-insights" style={{ padding: 16 }}>
        <h3>
          Daily Insights
          {summary?.isStale && <span style={{ fontSize: 12, color: '#f59e0b', marginLeft: 8 }}>outdated</span>}
        </h3>
        {summaryLoading ? (
          <SkeletonBlock lines={3} />
        ) : summaryError ? (
          <ErrorBanner message="Could not load insights." onRetry={loadSummary} />
        ) : (
          <ul>
            {insightsList.map((insight, i) => <li key={i}>{insight}</li>)}
          </ul>
        )}
      </div>

      {/* FAB */}
      <button
        onClick={() => setShowSheet(true)}
        style={{
          position: 'fixed', bottom: 24, right: 24,
          width: 56, height: 56, borderRadius: '50%',
          background: '#3b82f6', color: '#fff',
          fontSize: 28, border: 'none', cursor: 'pointer',
          boxShadow: '0 4px 12px rgba(0,0,0,0.2)'
        }}
      >
        +
      </button>

      {showSheet && (
        <AddMealSheet
          onClose={() => setShowSheet(false)}
          onJobReady={(_jobId) => {
            setShowSheet(false);
            loadMeals();
          }}
        />
      )}
    </div>
  );
}

// Simple DateOnly helper
class DateOnly {
  private readonly value: string;
  constructor(value: string) { this.value = value; }
  static today() { return new DateOnly(new Date().toISOString().split('T')[0]); }
  static parse(s: string) { return new DateOnly(s); }
  toString() { return this.value; }
  addDays(n: number) {
    const d = new Date(this.value + 'T00:00:00Z');
    d.setUTCDate(d.getUTCDate() + n);
    return new DateOnly(d.toISOString().split('T')[0]);
  }
}
