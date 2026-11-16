/*
 * BannerScene — animated harbour scene for dashboard banners.
 *
 * Layout (position:absolute, inset:0, behind banner text):
 *   Sea band  : full-width CSS-animated wave layers (SeaWaves / wave.svg)
 *   Stage SVG : ship + dock anchored bottom-right; ship makes voyage.
 *
 * Ship voyage uses motion/react (multi-keyframe with per-segment easing).
 * Ship art, dock art, and wave art all come from src/assets/svg/ via the
 * shared scene components. Wake trails remain inline — they are a small
 * layout effect, not artwork.
 *
 * All CSS scene keyframes (wave, ship-beacon, smoke, crane) live in scene.css.
 * BannerScene pauses them via .scene-paused class when off-screen or when the
 * tab is hidden, and respects prefers-reduced-motion.
 */
import { useEffect, useRef, useState } from 'react';
import { motion, useReducedMotion } from 'motion/react';
import Ship from '../scene/Ship';
import Dock from '../scene/Dock';
import { WaveBack, WaveMid, WaveFore } from '../scene/SeaWaves';
import '../scene/scene.css';

const CYCLE = 50;
const SHIP_START_X = -315;
const SHIP_TX = 340;
const SHIP_TY = 122;

const voyage = {
  x: {
    value: [SHIP_START_X, SHIP_START_X * 0.04, 0, 0, 0, SHIP_START_X, SHIP_START_X],
    times: [0, 0.76, 0.80, 0.94, 0.96, 0.98, 1],
    ease: ['linear', 'easeOut', 'linear', 'linear', 'linear', 'linear'],
  },
  opacity: {
    value: [1, 1, 0, 0, 1],
    times: [0, 0.93, 0.95, 0.98, 1],
    ease: 'linear',
  },
};

const wakeEnvelope = { value: [0, 0.9, 0.9, 0, 0], times: [0, 0.05, 0.76, 0.80, 1] };

function anim(playing, keyframes, transition, rest) {
  return playing
    ? { animate: keyframes, transition: { ...transition, repeat: Infinity } }
    : { animate: rest, transition: { duration: 0 } };
}

export default function BannerScene() {
  const ref = useRef(null);
  const reduce = useReducedMotion();
  const [mode, setMode] = useState('full');
  const [inView, setInView] = useState(true);
  const [tabVisible, setTabVisible] = useState(true);

  useEffect(() => {
    const banner = ref.current?.closest('.dash-banner');
    if (!banner) return undefined;
    const ro = new ResizeObserver(([entry]) => {
      const w = entry.contentRect.width;
      setMode(w < 640 ? 'sea' : w < 1000 ? 'compact' : 'full');
    });
    ro.observe(banner);
    return () => ro.disconnect();
  }, []);

  useEffect(() => {
    const el = ref.current;
    if (!el) return undefined;
    const io = new IntersectionObserver(
      ([entry]) => setInView(entry.isIntersecting),
      { threshold: 0 },
    );
    io.observe(el);
    const onVis = () => setTabVisible(!document.hidden);
    document.addEventListener('visibilitychange', onVis);
    return () => { io.disconnect(); document.removeEventListener('visibilitychange', onVis); };
  }, []);

  const visible = inView && tabVisible && !reduce;
  const seaPlaying = visible;
  const playing = mode === 'full' && visible;

  return (
    <div
      className={`dash-banner__scene${seaPlaying ? '' : ' scene-paused'}`}
      ref={ref}
      aria-hidden="true"
    >
      {/* Full-width sea band with three CSS-animated wave layers from wave.svg */}
      <svg
        className="dash-banner__sea"
        viewBox="0 0 1080 60"
        preserveAspectRatio="none"
        focusable="false"
      >
        {/* Background rect — lightest sea colour */}
        <rect width="1080" height="60" fill="#D3E7EE" />
        {/* Three wave layers; y positions create visible wave crests */}
        <WaveBack y={15} />
        <WaveMid  y={30} />
        <WaveFore y={45} />
      </svg>

      {/* Stage: ship + dock, anchored bottom-right */}
      {mode !== 'sea' && (
        <svg
          className={`dash-banner__stage${playing ? '' : ' scene-paused'}`}
          viewBox="0 0 900 260"
          preserveAspectRatio="xMaxYMax meet"
          focusable="false"
        >
          {/* Dock — anchored at right; grounded on its own jetty from dock.svg */}
          {mode === 'full' && (
            <g transform="translate(570 60) scale(0.5)">
              <Dock />
            </g>
          )}

          {/* Ship voyage (motion/react) + bob + wake trails */}
          <motion.g
            initial={false}
            animate={playing
              ? { x: voyage.x.value, opacity: voyage.opacity.value }
              : { x: 0, opacity: 1 }}
            transition={playing ? {
              x: { duration: CYCLE, times: voyage.x.times, ease: voyage.x.ease, repeat: Infinity },
              opacity: { duration: CYCLE, times: voyage.opacity.times, ease: voyage.opacity.ease, repeat: Infinity },
            } : { duration: 0 }}
          >
            {/* Wake trails — layout wrappers, not artwork from files */}
            <motion.g
              initial={false}
              {...anim(playing,
                { opacity: wakeEnvelope.value },
                { duration: CYCLE, times: wakeEnvelope.times, ease: 'linear' },
                { opacity: 0 },
              )}
            >
              <motion.g
                initial={false}
                {...anim(playing, { x: [0, -10, 0] }, { duration: 1.3, ease: 'easeInOut' }, { x: 0 })}
                stroke="#FFFFFF" strokeLinecap="round" fill="none"
              >
                <path d="M20 300 H-70" strokeWidth="6" opacity="0.85" />
                <path d="M30 312 H-50" strokeWidth="5" opacity="0.6" />
                <path d="M18 322 H-74" strokeWidth="5" opacity="0.45" />
              </motion.g>
            </motion.g>

            {/* Bob wrapper */}
            <motion.g
              style={{ transformBox: 'fill-box', transformOrigin: '50% 80%' }}
              initial={false}
              {...anim(playing,
                { y: [0, -2.5, 0], rotate: [-0.6, 0.6, -0.6] },
                { duration: 4, ease: 'easeInOut' },
                { y: 0, rotate: 0 },
              )}
            >
              {/*
               * Ship art from ship.svg via Ship.jsx.
               * ship.svg space is 860×340. Scale 0.3 placed at (340, 122).
               */}
              <g transform={`translate(${SHIP_TX} ${SHIP_TY}) scale(0.3)`}>
                <Ship />
              </g>
            </motion.g>
          </motion.g>
        </svg>
      )}
    </div>
  );
}
