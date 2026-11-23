const COLOMBO = 'Asia/Colombo';

const dateFormatter = new Intl.DateTimeFormat('en-GB', {
  timeZone: COLOMBO,
  day: 'numeric',
  month: 'short',
  year: 'numeric',
});

const dateFormatter2d = new Intl.DateTimeFormat('en-GB', {
  timeZone: COLOMBO,
  day: '2-digit',
  month: 'short',
  year: 'numeric',
});

const timeFormatter = new Intl.DateTimeFormat('en-GB', {
  timeZone: COLOMBO,
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

// "18 Nov 2026"
export function formatDate(iso) {
  if (!iso) return '—';
  try {
    return dateFormatter.format(new Date(iso));
  } catch {
    return '—';
  }
}

// "18 Nov 2026, 14:05"
export function formatDatetime(iso) {
  if (!iso) return '—';
  try {
    const d = new Date(iso);
    const datePart = dateFormatter2d.format(d);
    const timePart = timeFormatter.format(d);
    return `${datePart}, ${timePart}`;
  } catch {
    return '—';
  }
}

// Days until an ISO date, from now
export function daysUntil(iso) {
  if (!iso) return null;
  return Math.floor((new Date(iso) - Date.now()) / 86400000);
}
