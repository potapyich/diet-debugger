interface ErrorBannerProps {
  message?: string;
  onRetry?: () => void;
}

export function ErrorBanner({ message = 'Something went wrong.', onRetry }: ErrorBannerProps) {
  return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      gap: 12,
      padding: '12px 16px',
      background: '#fee2e2',
      borderRadius: 8,
      border: '1px solid #fca5a5',
    }}>
      <span style={{ fontSize: 14, color: '#dc2626' }}>{message}</span>
      {onRetry && (
        <button
          onClick={onRetry}
          style={{
            fontSize: 13,
            padding: '4px 12px',
            borderRadius: 6,
            border: '1px solid #dc2626',
            background: '#fff',
            color: '#dc2626',
            cursor: 'pointer',
            whiteSpace: 'nowrap',
          }}
        >
          Retry
        </button>
      )}
    </div>
  );
}
