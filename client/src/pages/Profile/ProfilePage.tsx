import { useEffect, useState } from 'react';
import api from '../../lib/axios';
import { useToast } from '../../contexts/ToastContext';

interface ProfileData {
  weightKg?: number;
  heightCm?: number;
  age?: number;
  sex?: string;
  dietType?: string;
  feedbackTone?: string;
  preferredLanguage?: string;
  profileCompletedAt?: string | null;
}

interface GoalData {
  goalType?: string;
  dailyCalorieTarget?: number;
  proteinTargetG?: number;
  fatTargetG?: number;
  carbsTargetG?: number;
}

interface UserHabit {
  id: string;
  habitDescription: string;
  createdAt: string;
}

const REQUIRED_PROFILE_FIELDS = ['weightKg', 'heightCm', 'age', 'sex'] as const;
const TOTAL_FIELDS = 7;

export function ProfilePage() {
  const { showToast } = useToast();
  const [profile, setProfile] = useState<ProfileData>({});
  const [goal, setGoal] = useState<GoalData>({});
  const [habits, setHabits] = useState<UserHabit[]>([]);
  const [newHabit, setNewHabit] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    api.get('/profile').then(r => setProfile(r.data)).catch(() => {});
    api.get('/goals').then(r => setGoal(r.data)).catch(() => {});
    api.get<UserHabit[]>('/habits').then(r => setHabits(r.data)).catch(() => {});
  }, []);

  const addHabit = async () => {
    const desc = newHabit.trim();
    if (!desc) return;
    try {
      const { data } = await api.post<UserHabit>('/habits', { habitDescription: desc });
      setHabits(prev => [...prev, data]);
      setNewHabit('');
    } catch {
      showToast('Error', 'Failed to add habit. Max 10 habits or text too long.');
    }
  };

  const deleteHabit = async (id: string) => {
    try {
      await api.delete(`/habits/${id}`);
      setHabits(prev => prev.filter(h => h.id !== id));
    } catch {
      showToast('Error', 'Failed to delete habit.');
    }
  };

  const filledCount = REQUIRED_PROFILE_FIELDS.filter(f => profile[f] != null).length
    + (goal.goalType != null ? 1 : 0)
    + (profile.preferredLanguage != null ? 1 : 0)
    + (profile.feedbackTone != null ? 1 : 0);

  const completionCount = Math.min(filledCount, TOTAL_FIELDS);

  const save = async () => {
    setSaving(true);
    try {
      await api.put('/profile', {
        weightKg: profile.weightKg,
        heightCm: profile.heightCm,
        age: profile.age,
        sex: profile.sex != null ? (['Male', 'Female', 'Other'].indexOf(profile.sex)) : undefined,
        dietType: profile.dietType != null ? (['Standard', 'Vegetarian', 'Vegan', 'Keto', 'Other'].indexOf(profile.dietType)) : undefined,
        feedbackTone: profile.feedbackTone != null ? (['Neutral', 'Direct', 'Harsh'].indexOf(profile.feedbackTone)) : undefined,
        preferredLanguage: profile.preferredLanguage,
      });
      if (goal.goalType != null) {
        await api.put('/goals', {
          goalType: ['Cut', 'Maintain', 'Bulk'].indexOf(goal.goalType ?? 'Cut'),
          dailyCalorieTarget: goal.dailyCalorieTarget ?? 2000,
          proteinTargetG: goal.proteinTargetG,
          fatTargetG: goal.fatTargetG,
          carbsTargetG: goal.carbsTargetG,
        });
      }
      // Also set Accept-Language header for future requests
      if (profile.preferredLanguage) {
        api.defaults.headers.common['Accept-Language'] = profile.preferredLanguage;
      }
      showToast('InsightsReady', 'Profile saved!');
    } catch {
      showToast('Error', 'Failed to save profile.');
    } finally {
      setSaving(false);
    }
  };

  const updateProfile = (field: keyof ProfileData, value: unknown) =>
    setProfile(prev => ({ ...prev, [field]: value }));

  const updateGoal = (field: keyof GoalData, value: unknown) =>
    setGoal(prev => ({ ...prev, [field]: value }));

  return (
    <div className="profile-page" style={{ padding: 16, maxWidth: 480, margin: '0 auto' }}>
      <h2>Profile</h2>

      {/* Completion indicator */}
      <div style={{ marginBottom: 24 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
          <span>Profile {completionCount}/{TOTAL_FIELDS} fields complete</span>
        </div>
        <div style={{ background: '#e5e7eb', height: 8, borderRadius: 4 }}>
          <div style={{
            background: '#3b82f6', height: 8, borderRadius: 4,
            width: `${(completionCount / TOTAL_FIELDS) * 100}%`,
            transition: 'width 0.3s'
          }} />
        </div>
      </div>

      {/* Body stats */}
      <section>
        <h3>Body Stats</h3>
        <label>Weight (kg)<br />
          <input type="number" value={profile.weightKg ?? ''} onChange={e => updateProfile('weightKg', e.target.value ? +e.target.value : undefined)} />
        </label>
        <label>Height (cm)<br />
          <input type="number" value={profile.heightCm ?? ''} onChange={e => updateProfile('heightCm', e.target.value ? +e.target.value : undefined)} />
        </label>
        <label>Age<br />
          <input type="number" value={profile.age ?? ''} onChange={e => updateProfile('age', e.target.value ? +e.target.value : undefined)} />
        </label>
        <label>Sex<br />
          <select value={profile.sex ?? ''} onChange={e => updateProfile('sex', e.target.value || undefined)}>
            <option value="">—</option>
            <option value="Male">Male</option>
            <option value="Female">Female</option>
            <option value="Other">Other</option>
          </select>
        </label>
      </section>

      {/* Goal */}
      <section>
        <h3>Goal</h3>
        <label>Goal type<br />
          <div style={{ display: 'flex', gap: 8 }}>
            {['Cut', 'Maintain', 'Bulk'].map(gt => (
              <label key={gt}>
                <input type="radio" name="goalType" value={gt} checked={goal.goalType === gt}
                  onChange={() => updateGoal('goalType', gt)} />
                {gt}
              </label>
            ))}
          </div>
        </label>
        <label>Daily calorie target<br />
          <input type="number" value={goal.dailyCalorieTarget ?? ''} onChange={e => updateGoal('dailyCalorieTarget', e.target.value ? +e.target.value : undefined)} />
        </label>
      </section>

      {/* Macros */}
      <section>
        <h3>Macro Targets (optional)</h3>
        <label>Protein (g)<br />
          <input type="number" value={goal.proteinTargetG ?? ''} onChange={e => updateGoal('proteinTargetG', e.target.value ? +e.target.value : undefined)} />
        </label>
        <label>Fat (g)<br />
          <input type="number" value={goal.fatTargetG ?? ''} onChange={e => updateGoal('fatTargetG', e.target.value ? +e.target.value : undefined)} />
        </label>
        <label>Carbs (g)<br />
          <input type="number" value={goal.carbsTargetG ?? ''} onChange={e => updateGoal('carbsTargetG', e.target.value ? +e.target.value : undefined)} />
        </label>
      </section>

      {/* Preferences */}
      <section>
        <h3>Preferences</h3>
        <label>Diet type<br />
          <select value={profile.dietType ?? ''} onChange={e => updateProfile('dietType', e.target.value || undefined)}>
            <option value="">—</option>
            {['Standard', 'Vegetarian', 'Vegan', 'Keto', 'Other'].map(dt => (
              <option key={dt} value={dt}>{dt}</option>
            ))}
          </select>
        </label>
        <label>Feedback tone<br />
          <div style={{ display: 'flex', gap: 8 }}>
            {['Neutral', 'Direct', 'Harsh'].map(tone => (
              <label key={tone}>
                <input type="radio" name="feedbackTone" value={tone} checked={profile.feedbackTone === tone}
                  onChange={() => updateProfile('feedbackTone', tone)} />
                {tone}
              </label>
            ))}
          </div>
        </label>
        <label>Language<br />
          <select value={profile.preferredLanguage ?? 'en'} onChange={e => updateProfile('preferredLanguage', e.target.value)}>
            <option value="en">English</option>
            <option value="ru">Русский</option>
          </select>
        </label>
      </section>

      {/* My Habits */}
      <section style={{ marginTop: 24 }}>
        <h3>My Habits</h3>
        <p style={{ fontSize: 13, color: '#6b7280', marginTop: 0 }}>
          Tell the AI about your known behavioral patterns so it can give more relevant advice.
        </p>
        {habits.map(h => (
          <div key={h.id} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
            <span style={{ flex: 1, fontSize: 14 }}>{h.habitDescription}</span>
            <button
              onClick={() => deleteHabit(h.id)}
              style={{ fontSize: 12, padding: '2px 8px', color: '#ef4444', border: '1px solid #fca5a5', borderRadius: 4, background: 'none', cursor: 'pointer' }}
            >
              Delete
            </button>
          </div>
        ))}
        {habits.length < 10 && (
          <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
            <input
              value={newHabit}
              onChange={e => setNewHabit(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && addHabit()}
              placeholder="e.g. I snack when stressed"
              maxLength={200}
              style={{ flex: 1, padding: '6px 10px', borderRadius: 6, border: '1px solid #d1d5db' }}
            />
            <button onClick={addHabit} disabled={!newHabit.trim()} style={{ padding: '6px 16px' }}>
              Add
            </button>
          </div>
        )}
        {habits.length >= 10 && (
          <p style={{ fontSize: 13, color: '#9ca3af' }}>Maximum 10 habits reached.</p>
        )}
      </section>

      <button onClick={save} disabled={saving} style={{ marginTop: 24, padding: '12px 32px' }}>
        {saving ? 'Saving...' : 'Save'}
      </button>
    </div>
  );
}
