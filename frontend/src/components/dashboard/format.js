// Small formatting helpers shared by the dashboard widgets.

// Token hex mirrors of tokens.css, for SVG fills/strokes (CSS custom properties
// do not resolve inside SVG presentation attributes, so recharts needs literals).
export const COLORS = {
  primary: '#1C4E5E',
  chart: '#2F6F7E',
  muted: '#536974',
  line: '#E1EAEF',
};


const MONTH_INITIALS = ['J', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D'];

export function monthInitial(month /* 1..12 */) {
  return MONTH_INITIALS[(month - 1) % 12];
}

const lkr = new Intl.NumberFormat('en-LK', {
  style: 'currency', currency: 'LKR', maximumFractionDigits: 0,
});

export function formatLkr(value) {
  return lkr.format(Number(value) || 0);
}

export function formatNumber(value) {
  return new Intl.NumberFormat('en-LK').format(Number(value) || 0);
}

// Human clearance duration; returns null when not derivable.
// e.g. 18 → "18 h", 18.5 → "18.5 h", 72 → "3 days", 57.6 → "2.4 days"
export function formatHours(hours) {
  if (hours == null) return null;
  if (hours < 48) return `${parseFloat(hours.toFixed(1))} h`;
  return `${parseFloat((hours / 24).toFixed(1))} days`;
}

// "19 Oct" for a yyyy-mm-dd week-start string.
export function formatWeekStart(dateStr) {
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return dateStr;
  return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short' });
}

// "15 Nov 2026" for an ISO date/timestamp.
export function formatDate(iso) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

// "15 Nov 2026, 14:30" for an ISO timestamp.
export function formatDateTime(iso) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  return d.toLocaleString('en-GB', {
    day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}
