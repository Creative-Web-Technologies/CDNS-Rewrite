import { useCallback, useEffect, useState } from 'react';
import { apiGet, apiPost, setUnauthorizedHandler } from '../lib/api';
import { AuthContext } from './AuthContext.js';

export function AuthProvider({ children }) {
  // user = { userName, firstName, lastName, memberType, portal } | null
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // Let the api client clear the user when any call (other than the auth
  // probe/login) comes back 401; the client then redirects to /login.
  useEffect(() => {
    setUnauthorizedHandler(() => setUser(null));
    return () => setUnauthorizedHandler(null);
  }, []);

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
