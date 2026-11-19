// Skeleton — a single shimmering placeholder block. `h`/`w` are CSS sizes.
export default function Skeleton({ h = 16, w = '100%', radius, style }) {
  return (
    <span
      className="dash-skel"
      aria-hidden="true"
      style={{ display: 'block', height: h, width: w, borderRadius: radius, ...style }}
    />
  );
}
