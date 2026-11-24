export function formatAmount(amount) {
  if (amount === 0) return 'FREE';
  return new Intl.NumberFormat('en-LK', {
    style: 'currency', currency: 'LKR', maximumFractionDigits: 2,
  }).format(amount);
}

export function formatDuration(n, type) {
  if (type === 'Lifetime') return 'Lifetime';
  if (type === 'Years')  return `${n} ${n === 1 ? 'Year'  : 'Years'}`;
  if (type === 'Months') return `${n} ${n === 1 ? 'Month' : 'Months'}`;
  return `${n} ${n === 1 ? 'Day' : 'Days'}`;
}
