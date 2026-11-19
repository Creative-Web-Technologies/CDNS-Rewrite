/*
 * SeaWaves — three animated ocean wave layers built from wave.svg.
 *
 * Each <WaveSvg> is a nested <svg> (standard svgr output) positioned
 * within the parent SVG via x/y/width/height attributes. The CSS
 * animation in scene.css targets `.cd-wave-* path`, which moves the
 * wave path left inside its own viewport for a seamless scrolling loop.
 *
 * wave.svg uses fill="currentColor". Each cd-wave-* class in scene.css
 * overrides that via `fill:` on the path selector.
 *
 * Individual exports (WaveBack, WaveMid, WaveFore) let callers control
 * z-ordering so the ship and dock can sit between wave layers.
 */
import WaveSvg from '../../assets/svg/wave.svg?react';

/* Wave SVG native size: viewBox 0 0 1080 126. Width 1200 leaves room so
   the rightmost fill is never clipped during the -120px translateX loop. */
const W = 1200;
const H = 130;

export function WaveBack({ y = 294 }) {
  return <WaveSvg className="cd-wave-a" y={y} width={W} height={H} aria-hidden="true" />;
}

export function WaveMid({ y = 306 }) {
  return <WaveSvg className="cd-wave-b" y={y} width={W} height={H} aria-hidden="true" />;
}

export function WaveFore({ y = 322 }) {
  return <WaveSvg className="cd-wave-c" y={y} width={W} height={H} aria-hidden="true" />;
}

export default function SeaWaves({ y1 = 294, y2 = 306, y3 = 322 }) {
  return (
    <>
      <WaveBack y={y1} />
      <WaveMid  y={y2} />
      <WaveFore y={y3} />
    </>
  );
}
