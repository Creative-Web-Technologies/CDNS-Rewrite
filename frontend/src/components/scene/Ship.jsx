/*
 * Ship — wraps ship.svg as a nested <svg> inside the parent scene SVG.
 * ship.svg (860×340) includes an internal mirror transform so the bow
 * faces right. Pass width/height for explicit sizing inside a <g> wrapper.
 *
 * Usage in an SVG:
 *   <g transform="translate(140 195) scale(0.3605)">
 *     <Ship width={860} height={340} />
 *   </g>
 *
 * The CSS classes in scene.css (#ship-smoke, #ship-beacon) animate internal
 * elements. SVGO preserves these IDs (cleanupIds: false in vite.config).
 */
import ShipSvg from '../../assets/svg/ship.svg?react';

export default function Ship({ width = 860, height = 340, ...props }) {
  return <ShipSvg width={width} height={height} aria-hidden="true" {...props} />;
}
