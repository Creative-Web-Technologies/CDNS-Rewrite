/*
 * Sun — wraps sun.svg as a nested <svg> inside the parent scene SVG.
 * Renders with cd-sun class so scene.css animates it across the sky.
 * sun.svg has cd-glare/cd-glare-b/cd-rays classes baked in for inner animations.
 * Pass x, y, width, height to position within the parent SVG.
 */
import SunSvg from '../../assets/svg/sun.svg?react';

export default function Sun({ x = 0, y = 0, width = 240, height = 192, ...props }) {
  return (
    <SunSvg
      x={x} y={y} width={width} height={height}
      className="cd-sun"
      aria-hidden="true"
      {...props}
    />
  );
}
