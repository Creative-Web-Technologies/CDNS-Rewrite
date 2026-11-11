import { useCallback, useEffect, useState } from 'react';
import { apiGet, apiPost } from '../lib/api';
import { AuthContext } from './AuthContext.js';

export function AuthProvider({ children }) {
  // user = { userName, firstName, lastName, memberType, portal } | null
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiGet('/api/auth/me')
      .then(data => setUser(data))
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  const logout = useCallback(async () => {
    try { await apiPost('/api/auth/logout', {}); } catch { /* best-effort */ }
    setUser(null);
  }, []);

  // Called by login/signup/activation pages after a successful auth response
  const setAuth = useCallback(userData => setUser(userData), []);

  return (
    <AuthContext.Provider value={{ user, loading, logout, setAuth }}>
      {children}
    </AuthContext.Provider>
  );
}
