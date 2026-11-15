import { useCallback, useEffect, useState } from 'react';
import { apiGet } from '../../lib/api';

// Fetches a dashboard summary, exposing a loading/ready/error status and a
// reload() for the inline error retry.
export function useDashboardSummary(url) {
  const [summary, setSummary] = useState(null);
  const [status, setStatus] = useState('loading'); // 'loading' | 'ready' | 'error'

  const load = useCallback(() => {
    let alive = true;
    setStatus('loading');
    apiGet(url)
      .then((data) => { if (alive) { setSummary(data); setStatus('ready'); } })
      .catch(() => { if (alive) setStatus('error'); });
    return () => { alive = false; };
  }, [url]);

  useEffect(() => load(), [load]);

  return { summary, status, reload: load };
}
