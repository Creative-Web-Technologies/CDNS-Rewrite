/*
 * Dock — wraps dock.svg as a nested <svg> inside the parent scene SVG.
 * dock.svg (660×340) has the CUSTOMS building, crane, flag, stacked containers.
 * The crane-hoist and dock-flag animations in scene.css target IDs inside this SVG.
 *
 * Usage in an SVG (position quay top at waterline):
 *   <g transform="translate(380 126) scale(0.515)">
 *     <Dock width={660} height={340} />
 *   </g>
 *
 * The scale factor places dock SVG y=296 (quay top) at scene y=278.
 */
import DockSvg from '../../assets/svg/dock.svg?react';

export default function Dock({ width = 660, height = 340, ...props }) {
  return <DockSvg width={width} height={height} aria-hidden="true" {...props} />;
}
