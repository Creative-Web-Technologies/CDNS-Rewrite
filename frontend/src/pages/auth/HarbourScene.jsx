/*
 * HarbourScene — full-bleed animated harbour SVG for the auth left panel.
 *
 * viewBox 0 0 720 420. The SVG has no sky rect so the panel's #E3F2FD
 * shows through, making the scene seamlessly full-bleed.
 *
 * Z-order (back to front):
 *   sun → clouds → gulls → wave-back → ship → dock → wave-mid → wave-fore → shimmer
 *
 * All animation keyframes live in scene.css.
 * All SVG artwork is imported from src/assets/svg/ via the shared scene components.
 */
import Sun from '../../components/scene/Sun';
import Cloud from '../../components/scene/Cloud';
import Gulls from '../../components/scene/Gulls';
import Shimmer from '../../components/scene/Shimmer';
import { WaveBack, WaveMid, WaveFore } from '../../components/scene/SeaWaves';
import Ship from '../../components/scene/Ship';
import Dock from '../../components/scene/Dock';
import '../../components/scene/scene.css';
import './HarbourScene.css';

export default function HarbourScene() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 720 420"
      preserveAspectRatio="xMidYMax slice"
      className="harbour-scene"
    >
      {/*
       * No sky background rect: the panel's --auth-panel (#E3F2FD) is
       * the sky. Transparent SVG makes the scene full-bleed with no box.
       */}

      {/* Sun drifts from left to right over 80s. sun.svg: 240×192. */}
      <Sun x={0} y={0} width={240} height={192} />

      {/* Cloud 1: slow drift. cloud.svg: 120×36. */}
      <Cloud x={230} y={58} width={120} height={36} />

      {/* Cloud 2: faster drift. */}
      <Cloud variant="b" x={468} y={26} width={96} height={28} />

      {/* Gulls: looping arc. gulls.svg: 68×32. */}
      <Gulls x={378} y={116} width={68} height={32} />

      {/* Back sea layer — behind ship and dock */}
      <WaveBack y={294} />

      {/*
       * Ship sails in from the left and bobs.
       * cd-ship: sail-in (translateX -420→0, opacity 0→1→0).
       * cd-bob: gentle bob with ±0.6° rotation.
       * ship.svg: 860×340. Scale 0.3605 → scene box 310×123 at (140,195).
       * Hull bottom lands at scene y≈313, below the waterline at y=294. ✓
       */}
      <g className="cd-ship">
        <g
          className="cd-bob"
          style={{ transformBox: 'fill-box', transformOrigin: 'center' }}
        >
          <g transform="translate(140 195) scale(0.3605)">
            <Ship width={860} height={340} />
          </g>
        </g>
      </g>

      {/*
       * Dock anchored bottom-right, grounded on its own jetty.
       * dock.svg: 660×340. scale=0.515 → 340×175 in scene.
       * translate(380,126): dock y=296 × 0.515 → scene y=278 (quay on waterline).
       * scene x: 380 to 380+340=720 (flush with scene right edge). ✓
       */}
      <g transform="translate(380 126) scale(0.515)">
        <Dock width={660} height={340} />
      </g>

      {/* Mid and fore wave layers — in front of dock quay */}
      <WaveMid  y={306} />
      <WaveFore y={322} />

      {/* Shimmer: sun-glare reflection. shimmer.svg: 72×48. Wrapped in cd-sun-x. */}
      <Shimmer x={84} y={332} width={72} height={48} />
    </svg>
  );
}
