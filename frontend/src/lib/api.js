export async function apiRequest(url, options = {}) {
  const headers = new Headers(options.headers);
  const token = localStorage.getItem('cdns_admin_token');

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(url, { ...options, headers });
  if (!response.ok) {
    let payload = null;
    try {
      payload = await response.json();
    } catch {
      // The response may not contain JSON.
    }

    const error = new Error(payload?.message || `Request failed (${response.status})`);
    error.status = response.status;
    error.details = payload;
    throw error;
  }

  if (response.status === 204) return null;
  return response.json();
}

export function apiPost(url, body) {
  return apiRequest(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
}

export function apiGet(url) {
  return apiRequest(url);
}
