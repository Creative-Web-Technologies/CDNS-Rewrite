/*
 * Cloud — wraps cloud.svg as a nested <svg>. Pass x, y, width, height.
 * variant="b" adds cd-cloud-b for a slower drift speed.
 * cloud.svg native size: viewBox 0 0 120 36.
 */
import CloudSvg from '../../assets/svg/cloud.svg?react';

export default function Cloud({ variant, x = 0, y = 0, width = 120, height = 36, ...props }) {
  return (
    <CloudSvg
      x={x} y={y} width={width} height={height}
      className={`cd-cloud${variant === 'b' ? ' cd-cloud-b' : ''}`}
      aria-hidden="true"
      {...props}
    />
  );
}
