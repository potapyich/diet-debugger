interface EmptyStateProps {
  icon?: string;
  heading: string;
  subtext?: string;
  ctaLabel?: string;
  onCta?: () => void;
}

export function EmptyState({ icon = '🍽️', heading, subtext, ctaLabel, onCta }: EmptyStateProps) {
  return (
    <div style={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      textAlign: 'center',
      padding: '32px 16px',
      gap: 8,
    }}>
      <span style={{ fontSize: 48, lineHeight: 1 }}>{icon}</span>
      <h3 style={{ margin: 0, fontSize: 18, fontWeight: 600, color: '#111827' }}>{heading}</h3>
      {subtext && <p style={{ margin: 0, fontSize: 14, color: '#6b7280', maxWidth: 260 }}>{subtext}</p>}
      {ctaLabel && onCta && (
        <button
          onClick={onCta}
          style={{
            marginTop: 8,
            padding: '10px 24px',
            borderRadius: 8,
            border: 'none',
            background: '#3b82f6',
            color: '#fff',
            fontSize: 14,
            fontWeight: 600,
            cursor: 'pointer',
          }}
        >
          {ctaLabel}
        </button>
      )}
    </div>
  );
}
