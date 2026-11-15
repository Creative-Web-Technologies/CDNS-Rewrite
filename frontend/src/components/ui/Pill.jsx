// Pill — a small status chip. `tone` selects one of the token pill palettes
// (green, amber, red, blue, grey).
export default function Pill({ tone = 'grey', children, className = '' }) {
  return (
    <span className={`ui-pill ui-pill--${tone} ${className}`}>
      {children}
    </span>
  );
}
