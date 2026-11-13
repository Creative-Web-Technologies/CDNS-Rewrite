import { useCallback, useEffect, useState } from 'react';
import { apiGet, apiPost } from '../lib/api';
import { AuthContext } from './AuthContext.js';

export function AuthProvider({ children }) {
  // user = { userName, firstName, lastName, memberType, portal } | null
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  const refreshUser = useCallback(async () => {
    try {
      const data = await apiGet('/api/auth/me');
      setUser(data);
      return data;
    } catch {
      setUser(null);
      return null;
    }
  }, []);

  useEffect(() => {
    refreshUser().finally(() => setLoading(false));
  }, [refreshUser]);

  const logout = useCallback(async () => {
    try { await apiPost('/api/auth/logout', {}); } catch { /* best-effort */ }
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, loading, logout, refreshUser }}>
      {children}
    </AuthContext.Provider>
  );
}
