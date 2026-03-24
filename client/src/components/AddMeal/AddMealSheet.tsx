import { useRef, useState } from 'react';
import api from '../../lib/axios';

interface AddMealSheetProps {
  onClose: () => void;
  onJobReady: (jobId: string) => void;
}

export function AddMealSheet({ onClose, onJobReady }: AddMealSheetProps) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [text, setText] = useState('');
  const photoRef = useRef<HTMLInputElement>(null);
  const galleryRef = useRef<HTMLInputElement>(null);

  const submitFile = async (file: File) => {
    setLoading(true);
    setError(null);
    try {
      const form = new FormData();
      form.append('image', file);
      const { data } = await api.post('/meals/analyze', form);
      await pollJob(data.jobId);
    } catch {
      setLoading(false);
      setError('Failed to analyze photo. Please try again.');
    }
  };

  const submitText = async () => {
    if (!text.trim()) return;
    setLoading(true);
    setError(null);
    try {
      const { data } = await api.post('/meals/analyze', { text });
      await pollJob(data.jobId);
    } catch {
      setLoading(false);
      setError('Failed to analyze. Please try again.');
    }
  };

  const pollJob = async (jobId: string) => {
    const interval = setInterval(async () => {
      try {
        const { data } = await api.get(`/meals/analyze/${jobId}`);
        if (data.status === 'ready') {
          clearInterval(interval);
          setLoading(false);
          onJobReady(jobId);
        } else if (data.status === 'failed') {
          clearInterval(interval);
          setLoading(false);
          setError(data.error || 'Analysis failed.');
        }
      } catch {
        clearInterval(interval);
        setLoading(false);
        setError('Connection error. Please retry.');
      }
    }, 1500);
  };

  return (
    <div className="bottom-sheet">
      {loading && (
        <div className="spinner-overlay">
          <div className="spinner" />
          <p>Analyzing your meal...</p>
        </div>
      )}

      {error && (
        <div className="error-banner">
          <p>{error}</p>
          <button onClick={() => setError(null)}>Retry</button>
        </div>
      )}

      {!loading && (
        <>
          <button onClick={() => photoRef.current?.click()}>
            Take Photo
          </button>
          <input
            ref={photoRef}
            type="file"
            accept="image/*"
            capture="environment"
            style={{ display: 'none' }}
            onChange={(e) => e.target.files?.[0] && submitFile(e.target.files[0])}
          />

          <button onClick={() => galleryRef.current?.click()}>
            Choose from Gallery
          </button>
          <input
            ref={galleryRef}
            type="file"
            accept="image/*"
            style={{ display: 'none' }}
            onChange={(e) => e.target.files?.[0] && submitFile(e.target.files[0])}
          />

          <div>
            <textarea
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder="Describe your meal..."
              rows={3}
            />
            <button onClick={submitText} disabled={!text.trim()}>
              Submit
            </button>
          </div>

          <button onClick={onClose}>Cancel</button>
        </>
      )}
    </div>
  );
}
