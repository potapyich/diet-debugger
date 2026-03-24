interface FeedbackCardProps {
  status: 'ok' | 'warning' | 'issue';
  insights: string[];
}

export function FeedbackCard({ status, insights }: FeedbackCardProps) {
  const icon = status === 'ok' ? '✓' : status === 'warning' ? '⚠' : '✗';
  return (
    <div className={`feedback-card feedback-${status}`}>
      <span className="feedback-icon">{icon}</span>
      <div>
        {insights.map((insight, i) => <p key={i}>{insight}</p>)}
      </div>
    </div>
  );
}
