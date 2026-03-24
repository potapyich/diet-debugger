import { useNavigate } from 'react-router-dom';

interface ProfilePromptProps {
  onDismiss: () => void;
}

export function ProfilePrompt({ onDismiss }: ProfilePromptProps) {
  const navigate = useNavigate();

  const goToProfile = () => {
    onDismiss();
    navigate('/profile');
  };

  return (
    <div style={{
      position: 'fixed',
      inset: 0,
      background: 'rgba(0,0,0,0.5)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      zIndex: 999,
      padding: 16,
    }}>
      <div style={{
        background: '#fff',
        borderRadius: 16,
        padding: 32,
        maxWidth: 360,
        width: '100%',
        textAlign: 'center',
      }}>
        <span style={{ fontSize: 48 }}>🥗</span>
        <h2 style={{ margin: '12px 0 8px', fontSize: 20, fontWeight: 700 }}>
          Get personalised insights
        </h2>
        <p style={{ fontSize: 14, color: '#6b7280', margin: '0 0 24px' }}>
          Set up your profile — height, weight, and goals — for AI analysis tailored to you.
        </p>
        <button
          onClick={goToProfile}
          style={{
            display: 'block',
            width: '100%',
            padding: '12px',
            borderRadius: 8,
            border: 'none',
            background: '#3b82f6',
            color: '#fff',
            fontSize: 15,
            fontWeight: 600,
            cursor: 'pointer',
            marginBottom: 12,
          }}
        >
          Set up profile
        </button>
        <button
          onClick={onDismiss}
          style={{
            display: 'block',
            width: '100%',
            padding: '12px',
            borderRadius: 8,
            border: '1px solid #d1d5db',
            background: '#fff',
            fontSize: 15,
            cursor: 'pointer',
            color: '#374151',
          }}
        >
          Skip for now
        </button>
      </div>
    </div>
  );
}
