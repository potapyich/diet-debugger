import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../lib/axios';
import { FeedbackCard } from './FeedbackCard';
import { useToast } from '../../contexts/ToastContext';
import type { PatternEvent } from '../PatternEventCard/PatternEventCard';

interface Ingredient {
  name: string;
  amount: string;
}

interface MealResultData {
  ingredients: Ingredient[];
  caloriesEstimate: number;
  proteinG: number;
  fatG: number;
  carbsG: number;
  portionEstimate: string;
  confidence: number;
}

interface MealResultScreenProps {
  result: MealResultData;
}

export function MealResultScreen({ result }: MealResultScreenProps) {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [ingredients, setIngredients] = useState<Ingredient[]>(result.ingredients);
  const [feedback, setFeedback] = useState<{ status: 'ok' | 'warning' | 'issue'; insights: string[] } | null>(null);
  const [saving, setSaving] = useState(false);

  const confidence = result.confidence;
  const confidenceBadgeClass =
    confidence >= 0.7 ? 'confidence-green' :
    confidence >= 0.4 ? 'confidence-yellow' :
    'confidence-red';

  const updateIngredient = (idx: number, field: 'name' | 'amount', value: string) => {
    setIngredients(prev => prev.map((ing, i) => i === idx ? { ...ing, [field]: value } : ing));
  };

  const save = async () => {
    setSaving(true);
    const savedAt = Date.now();
    try {
      const { data } = await api.post('/meals', {
        calories: result.caloriesEstimate,
        proteinG: result.proteinG,
        fatG: result.fatG,
        carbsG: result.carbsG,
        ingredients: JSON.stringify(ingredients),
        portionEstimate: result.portionEstimate,
        confidenceScore: confidence,
        source: 0, // Photo
      });

      // Fetch new pattern events (triggered within the last 10s)
      try {
        const { data: eventsData } = await api.get<PatternEvent[]>('/pattern-events?limit=5');
        const newEvents = eventsData.filter(
          e => !e.acknowledged && new Date(e.triggeredAt).getTime() >= savedAt - 10000
        );
        for (const ev of newEvents) {
          showToast(
            ev.type === 'Warning' ? 'PatternAlert' : 'StreakAlert',
            ev.message,
            6000
          );
        }
      } catch {
        // Pattern events are non-critical — ignore errors
      }

      if (data.feedback) {
        setFeedback(data.feedback);
        setTimeout(() => navigate('/day'), 3000);
      } else {
        navigate('/day');
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="meal-result-screen">
      {confidence < 0.7 && (
        <div className="confidence-banner">
          We&apos;re not sure — please check or retake the photo
        </div>
      )}

      <div className={`confidence-badge ${confidenceBadgeClass}`}>
        Confidence: {Math.round(confidence * 100)}%
      </div>

      <div className="calories-display">
        <strong>{Math.round(result.caloriesEstimate)}</strong> kcal
      </div>

      <div className="macro-bars">
        <div className="macro-bar">
          <span>Protein</span>
          <span>{result.proteinG.toFixed(1)}g</span>
        </div>
        <div className="macro-bar">
          <span>Fat</span>
          <span>{result.fatG.toFixed(1)}g</span>
        </div>
        <div className="macro-bar">
          <span>Carbs</span>
          <span>{result.carbsG.toFixed(1)}g</span>
        </div>
      </div>

      <div className="ingredients-list">
        <h3>Ingredients</h3>
        {ingredients.map((ing, idx) => (
          <div key={idx} className="ingredient-item">
            <input
              value={ing.name}
              onChange={(e) => updateIngredient(idx, 'name', e.target.value)}
            />
            <input
              value={ing.amount}
              onChange={(e) => updateIngredient(idx, 'amount', e.target.value)}
            />
          </div>
        ))}
      </div>

      {feedback && (
        <FeedbackCard status={feedback.status} insights={feedback.insights} />
      )}

      <button onClick={save} disabled={saving}>
        {saving ? 'Saving...' : 'Save'}
      </button>
      <button onClick={() => navigate(-1)}>Cancel</button>
    </div>
  );
}
