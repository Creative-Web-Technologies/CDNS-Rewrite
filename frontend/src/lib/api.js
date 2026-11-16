// Shared fetch client for the portals. Every call sends the session cookie
// (credentials: 'include') and throws an Error carrying { status, code, field }
// on a non-2xx response, so callers can branch on the server's error shape.

// Calls that must NOT trigger the global 401 redirect: the auth probe (/me is
// expected to 401 when logged out and is handled by AuthContext) and login
// (a 401 there means bad credentials, surfaced on the login form itself).
const UNAUTHORIZED_EXEMPT = ['/api/auth/me', '/api/auth/login'];

// AuthContext registers a handler here so a 401 can clear the in-memory user
// before we hard-navigate to /login.
let onUnauthorized = null;
export function setUnauthorizedHandler(handler) {
  onUnauthorized = handler;
}

function handleUnauthorized(url) {
  if (UNAUTHORIZED_EXEMPT.some((path) => url.startsWith(path))) return;

  // Clear the current user, then send the browser to /login. The full
  // navigation also resets any stale app state tied to the expired session.
  if (onUnauthorized) onUnauthorized();
  if (window.location.pathname !== '/login') {
    window.location.assign('/login');
  }
}

async function request(url, options) {
  const res = await fetch(url, { credentials: 'include', ...options });

  if (res.status === 401) {
    handleUnauthorized(url);
  }

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    const error = new Error(data.message || `HTTP ${res.status}`);
    error.status = res.status;
    error.code = data.code;
    error.field = data.field;
    throw error;
  }

  return res.json().catch(() => null);
}

export function apiGet(url) {
  return request(url, { method: 'GET' });
}

export function apiPost(url, body) {
  return request(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
}

export function apiPut(url, body) {
  return request(url, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
}

export function apiDelete(url, body) {
  return request(url, {
    method: 'DELETE',
    ...(body === undefined
      ? {}
      : { headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
  });
}
