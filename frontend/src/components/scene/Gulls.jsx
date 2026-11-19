/*
 * Gulls — wraps gulls.svg as a nested <svg>. Pass x, y, width, height.
 * gulls.svg native size: viewBox 0 0 68 32.
 */
import GullsSvg from '../../assets/svg/gulls.svg?react';

export default function Gulls({ x = 0, y = 0, width = 68, height = 32, ...props }) {
  return (
    <GullsSvg
      x={x} y={y} width={width} height={height}
      className="cd-gull"
      aria-hidden="true"
      {...props}
    />
  );
}
