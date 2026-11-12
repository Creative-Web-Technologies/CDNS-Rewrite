export async function apiGet(url) {
  const res = await fetch(url, {
    method: 'GET',
    credentials: 'include',
  });
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    const error = new Error(data.message || `HTTP ${res.status}`);
    error.status = res.status;
    error.code = data.code;
    throw error;
  }
  return res.json().catch(() => null);
}

export async function apiPost(url, body) {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    const error = new Error(body.message || `HTTP ${res.status}`);
    error.code = body.code;
    error.field = body.field;
    throw error;
  }
  return res.json().catch(() => null);
}
