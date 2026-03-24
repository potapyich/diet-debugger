interface CalorieRingProps {
  consumed: number;
  target: number;
}

export function CalorieRing({ consumed, target }: CalorieRingProps) {
  const radius = 54;
  const circumference = 2 * Math.PI * radius;
  const progress = target > 0 ? Math.min(consumed / target, 1) : 0;
  const offset = circumference * (1 - progress);

  return (
    <div className="calorie-ring-container" style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      <svg width={128} height={128} viewBox="0 0 128 128">
        <circle cx={64} cy={64} r={radius} fill="none" stroke="#e5e7eb" strokeWidth={12} />
        <circle
          cx={64} cy={64} r={radius}
          fill="none" stroke="#3b82f6" strokeWidth={12}
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
          transform="rotate(-90 64 64)"
          style={{ transition: 'stroke-dashoffset 0.5s ease' }}
        />
        <text x={64} y={60} textAnchor="middle" fill="#111" fontSize={18} fontWeight="bold">
          {Math.round(consumed)}
        </text>
        <text x={64} y={78} textAnchor="middle" fill="#6b7280" fontSize={12}>
          / {target} kcal
        </text>
      </svg>
    </div>
  );
}
