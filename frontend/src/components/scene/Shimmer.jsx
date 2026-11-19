/*
 * Shimmer — wraps shimmer.svg with a cd-sun-x tracking wrapper.
 * The outer <g class="cd-sun-x"> translates by 440px over 80s to track the
 * sun's horizontal drift. The inner shimmer SVG fades in/out.
 * Pass x, y, width, height to position within the parent SVG.
 * shimmer.svg native size: viewBox 0 0 72 48.
 */
import ShimmerSvg from '../../assets/svg/shimmer.svg?react';

export default function Shimmer({ x = 0, y = 0, width = 72, height = 48, ...props }) {
  return (
    <g className="cd-sun-x" aria-hidden="true">
      <ShimmerSvg
        x={x} y={y} width={width} height={height}
        className="cd-shimmer"
        {...props}
      />
    </g>
  );
}
