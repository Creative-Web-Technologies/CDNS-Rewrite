import { useEffect, useRef, useState } from 'react';
import { motion, useReducedMotion } from 'motion/react';

/*
 * BannerScene — a decorative harbour scene that is part of the whole banner.
 *
 * Two layers:
 *   1. Sea (full width): a pale band + drifting wave lines along the bottom,
 *      edge to edge, behind the text. Clipped to the banner's rounded corners by
 *      the banner's own overflow.
 *   2. Stage (right only): the dock and ship, inlined from src/assets/svg (not
 *      redrawn). Confined to the right of the text; all stage content lives
 *      within viewBox x[0..900] so no straight edge ever clips it.
 *
 * Stage coordinate space (viewBox 0 0 900 260): waterline y=210; dock at
 * translate(570,60) scale(0.5) — quay wall at x=600, building flush to x=900,
 * quay/ground extended down to the bottom (y=260). Ship at scale 0.3 docked at
 * translate(340,122), bow just short of the jetty; sails in from the left of the
 * stage (its hull always within viewBox x≥0, so it is never clipped and never
 * reaches the text).
 */

const CYCLE = 50;                 // full ship loop, seconds
// Voyage start offset: far enough left to read as "sailing in", but not so far
// that the ship (or its wake, which reaches ~22px left of the hull) crosses
// viewBox x=0 — so nothing is ever clipped by the stage edge or reaches the text.
const SHIP_START_X = -315;
const SHIP_TX = 340;
const SHIP_TY = 122;

// Ship voyage: sail in (linear, easing to a stop), wait, fade out, reset, fade in.
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

// Wake is only visible while the ship is sailing.
const wakeEnvelope = { value: [0, 0.9, 0.9, 0, 0], times: [0, 0.05, 0.76, 0.80, 1] };

// Crane lowers to the ship's top row, pauses, lifts — all inside the 7s wait.
const hoist = { value: [1, 1, 2, 2, 1, 1], times: [0, 0.80, 0.85, 0.90, 0.94, 1] };

// Full-width sea waves (viewBox 0 0 1000 60, stretched): periodic over 100 units
// so a -100 drift loops seamlessly; drawn past both edges so none ends abruptly.
function seaWave(y, amp) {
  let d = `M -200 ${y} q 25 ${-amp} 50 0`;
  for (let x = -150; x < 1200; x += 50) d += ' t 50 0';
  return d;
}
const WAVES = [
  { y: 24, amp: 4, phase: 0, opacity: 0.5 },
  { y: 34, amp: 5, phase: 26, opacity: 0.7 },
  { y: 44, amp: 3, phase: 60, opacity: 0.55 },
  { y: 52, amp: 4, phase: 14, opacity: 0.4 },
];

// Helper: loop when playing, otherwise snap to a resting value.
function anim(playing, keyframes, transition, rest) {
  return playing
    ? { animate: keyframes, transition: { ...transition, repeat: Infinity } }
    : { animate: rest, transition: { duration: 0 } };
}

// ── Static art (inlined from the source SVGs; groups preserved) ───────────────

function ShipBody() {
  return (
    <g id="ship-body">
      <g stroke="#6B8794" strokeWidth="5" strokeLinecap="round">
        <line x1="750" y1="100" x2="750" y2="54" /><line x1="736" y1="74" x2="764" y2="74" />
      </g>
      <path d="M796 104h18l-5 56h-13z" fill="#C4614B" stroke="#7A3526" strokeWidth="3" />
      <rect x="794" y="100" width="22" height="6" fill="#173642" />
      <rect x="710" y="100" width="80" height="92" rx="7" fill="#EEF4F6" stroke="#6B8794" strokeWidth="4" />
      <g fill="#3F6B80">
        <rect x="719" y="113" width="14" height="12" rx="2" /><rect x="743" y="113" width="14" height="12" rx="2" /><rect x="767" y="113" width="14" height="12" rx="2" />
      </g>
      <g stroke="#6B8794" strokeWidth="4" strokeLinecap="round">
        <line x1="50" y1="245" x2="50" y2="202" /><line x1="41" y1="218" x2="59" y2="218" />
      </g>
      <circle cx="50" cy="198" r="4" fill="#D9694F" />
      <g id="ship-containers">
        <rect x="110" y="194" width="96" height="49" rx="4" fill="#3A6E82" stroke="#1F4A5B" strokeWidth="3" />
        <g stroke="#1F4A5B" strokeWidth="2" opacity="0.7"><line x1="134" y1="199" x2="134" y2="238" /><line x1="158" y1="199" x2="158" y2="238" /><line x1="182" y1="199" x2="182" y2="238" /></g>
        <rect x="214" y="194" width="100" height="49" rx="4" fill="#D26E55" stroke="#9A4634" strokeWidth="3" />
        <g stroke="#9A4634" strokeWidth="2" opacity="0.7"><line x1="239" y1="199" x2="239" y2="238" /><line x1="264" y1="199" x2="264" y2="238" /><line x1="289" y1="199" x2="289" y2="238" /></g>
        <rect x="322" y="194" width="104" height="49" rx="4" fill="#4E6675" stroke="#33495A" strokeWidth="3" />
        <g stroke="#33495A" strokeWidth="2" opacity="0.7"><line x1="348" y1="199" x2="348" y2="238" /><line x1="374" y1="199" x2="374" y2="238" /><line x1="400" y1="199" x2="400" y2="238" /></g>
        <rect x="434" y="194" width="96" height="49" rx="4" fill="#D99A62" stroke="#A06A38" strokeWidth="3" />
        <g stroke="#A06A38" strokeWidth="2" opacity="0.7"><line x1="458" y1="199" x2="458" y2="238" /><line x1="482" y1="199" x2="482" y2="238" /><line x1="506" y1="199" x2="506" y2="238" /></g>
        <rect x="538" y="194" width="104" height="49" rx="4" fill="#3F7489" stroke="#1F4A5B" strokeWidth="3" />
        <g stroke="#1F4A5B" strokeWidth="2" opacity="0.7"><line x1="564" y1="199" x2="564" y2="238" /><line x1="590" y1="199" x2="590" y2="238" /><line x1="616" y1="199" x2="616" y2="238" /></g>
        <rect x="650" y="194" width="56" height="49" rx="4" fill="#D9B88E" stroke="#A0845C" strokeWidth="3" />
        <g stroke="#A0845C" strokeWidth="2" opacity="0.7"><line x1="664" y1="199" x2="664" y2="238" /><line x1="678" y1="199" x2="678" y2="238" /><line x1="692" y1="199" x2="692" y2="238" /></g>
        <rect x="170" y="142" width="100" height="51" rx="4" fill="#D26E55" stroke="#9A4634" strokeWidth="3" />
        <g stroke="#9A4634" strokeWidth="2" opacity="0.7"><line x1="195" y1="147" x2="195" y2="188" /><line x1="220" y1="147" x2="220" y2="188" /><line x1="245" y1="147" x2="245" y2="188" /></g>
        <rect x="278" y="142" width="100" height="51" rx="4" fill="#2B5A6C" stroke="#173C4A" strokeWidth="3" />
        <g stroke="#173C4A" strokeWidth="2" opacity="0.7"><line x1="303" y1="147" x2="303" y2="188" /><line x1="328" y1="147" x2="328" y2="188" /><line x1="353" y1="147" x2="353" y2="188" /></g>
        <rect x="386" y="142" width="96" height="51" rx="4" fill="#577986" stroke="#3A5663" strokeWidth="3" />
        <g stroke="#3A5663" strokeWidth="2" opacity="0.7"><line x1="410" y1="147" x2="410" y2="188" /><line x1="434" y1="147" x2="434" y2="188" /><line x1="458" y1="147" x2="458" y2="188" /></g>
        <rect x="490" y="142" width="108" height="51" rx="4" fill="#3C8196" stroke="#22566A" strokeWidth="3" />
        <g stroke="#22566A" strokeWidth="2" opacity="0.7"><line x1="517" y1="147" x2="517" y2="188" /><line x1="544" y1="147" x2="544" y2="188" /><line x1="571" y1="147" x2="571" y2="188" /></g>
        <rect x="606" y="158" width="96" height="35" rx="4" fill="#E08266" stroke="#A4513C" strokeWidth="3" />
        <g stroke="#A4513C" strokeWidth="2" opacity="0.7"><line x1="630" y1="163" x2="630" y2="188" /><line x1="654" y1="163" x2="654" y2="188" /><line x1="678" y1="163" x2="678" y2="188" /></g>
      </g>
      <rect x="60" y="243" width="772" height="9" fill="#173642" />
      <path d="M15 250H845L812 312Q804 328 780 328H150Q70 322 15 250Z" fill="#23485A" stroke="#12303D" strokeWidth="4" strokeLinejoin="round" />
      <path d="M112 314H797L780 326H150Z" fill="#C4614B" />
    </g>
  );
}

function DockArt({ playing }) {
  return (
    <g transform="translate(570 60) scale(0.5)">
      {/* Solid quay + ground, carried down to the bottom (y=400 → banner bottom)
          so nothing floats; the sea meets the quay's left face at x=60. */}
      <g id="dock-ground">
        <rect x="60" y="300" width="600" height="100" fill="#5E7C88" />
        <rect x="60" y="352" width="600" height="48" fill="#44626E" opacity="0.3" />
        <rect x="60" y="296" width="600" height="8" rx="2" fill="#44626E" />
        <g stroke="#44626E" strokeWidth="3" opacity="0.5"><path d="M60 318h600M60 334h600" /></g>
        <g fill="#23485A"><rect x="150" y="284" width="12" height="14" rx="3" /><rect x="360" y="284" width="12" height="14" rx="3" /></g>
      </g>

      {/* Wooden jetty stepping out towards the ship, on pilings into the water. */}
      <g id="dock-jetty">
        <g fill="#8B6F47">
          <rect x="-26" y="302" width="7" height="54" rx="2" /><rect x="-6" y="302" width="7" height="54" rx="2" />
          <rect x="16" y="302" width="7" height="54" rx="2" /><rect x="38" y="302" width="7" height="54" rx="2" />
        </g>
        <rect x="-34" y="292" width="100" height="12" rx="2" fill="#C9A77C" stroke="#A8865A" strokeWidth="2" />
        <g fill="#44626E"><rect x="-24" y="283" width="9" height="12" rx="3" /><rect x="40" y="283" width="9" height="12" rx="3" /></g>
      </g>

      <g id="dock-building">
        <rect x="446" y="96" width="194" height="204" rx="6" fill="#EEF4F6" stroke="#6B8794" strokeWidth="4" />
        <rect x="446" y="96" width="194" height="30" rx="6" fill="#1C4E5E" />
        <text x="543" y="117" textAnchor="middle" fontFamily="Inter, Arial, sans-serif" fontSize="16" fontWeight="700" letterSpacing="3" fill="#FFFFFF">CUSTOMS</text>
        <g fill="#3F6B80">
          <rect x="462" y="138" width="22" height="26" rx="3" /><rect x="496" y="138" width="22" height="26" rx="3" /><rect x="530" y="138" width="22" height="26" rx="3" /><rect x="564" y="138" width="22" height="26" rx="3" /><rect x="598" y="138" width="22" height="26" rx="3" />
          <rect x="462" y="178" width="22" height="26" rx="3" /><rect x="496" y="178" width="22" height="26" rx="3" /><rect x="530" y="178" width="22" height="26" rx="3" /><rect x="564" y="178" width="22" height="26" rx="3" /><rect x="598" y="178" width="22" height="26" rx="3" />
          <rect x="462" y="218" width="22" height="26" rx="3" /><rect x="496" y="218" width="22" height="26" rx="3" /><rect x="530" y="218" width="22" height="26" rx="3" /><rect x="564" y="218" width="22" height="26" rx="3" /><rect x="598" y="218" width="22" height="26" rx="3" />
        </g>
        <rect x="526" y="258" width="34" height="42" rx="3" fill="#23485A" />
        <rect x="436" y="88" width="214" height="10" rx="3" fill="#6B8794" />
      </g>

      <g id="dock-flag">
        <line x1="543" y1="88" x2="543" y2="30" stroke="#6B8794" strokeWidth="4" strokeLinecap="round" />
        <motion.path
          d="M545 32h44l-10 12 10 12h-44z" fill="#D9694F" stroke="#9A4634" strokeWidth="2" strokeLinejoin="round"
          style={{ transformBox: 'fill-box', transformOrigin: '0% 50%' }}
          initial={false}
          {...anim(playing, { rotate: [-3, 5, -3] }, { duration: 2.6, ease: 'easeInOut' }, { rotate: 0 })}
        />
      </g>

      <g id="dock-stack">
        <rect x="236" y="251" width="92" height="47" rx="4" fill="#3A6E82" stroke="#1F4A5B" strokeWidth="3" />
        <g stroke="#1F4A5B" strokeWidth="2" opacity="0.7"><line x1="259" y1="256" x2="259" y2="293" /><line x1="282" y1="256" x2="282" y2="293" /><line x1="305" y1="256" x2="305" y2="293" /></g>
        <rect x="332" y="251" width="92" height="47" rx="4" fill="#D99A62" stroke="#A06A38" strokeWidth="3" />
        <g stroke="#A06A38" strokeWidth="2" opacity="0.7"><line x1="355" y1="256" x2="355" y2="293" /><line x1="378" y1="256" x2="378" y2="293" /><line x1="401" y1="256" x2="401" y2="293" /></g>
        <rect x="236" y="202" width="92" height="47" rx="4" fill="#D26E55" stroke="#9A4634" strokeWidth="3" />
        <g stroke="#9A4634" strokeWidth="2" opacity="0.7"><line x1="259" y1="207" x2="259" y2="244" /><line x1="282" y1="207" x2="282" y2="244" /><line x1="305" y1="207" x2="305" y2="244" /></g>
        <rect x="332" y="202" width="92" height="47" rx="4" fill="#4E6675" stroke="#33495A" strokeWidth="3" />
        <g stroke="#33495A" strokeWidth="2" opacity="0.7"><line x1="355" y1="207" x2="355" y2="244" /><line x1="378" y1="207" x2="378" y2="244" /><line x1="401" y1="207" x2="401" y2="244" /></g>
        <rect x="284" y="153" width="92" height="47" rx="4" fill="#3C8196" stroke="#22566A" strokeWidth="3" />
        <g stroke="#22566A" strokeWidth="2" opacity="0.7"><line x1="307" y1="158" x2="307" y2="195" /><line x1="330" y1="158" x2="330" y2="195" /><line x1="353" y1="158" x2="353" y2="195" /></g>
      </g>

      <g id="dock-crane">
        <g stroke="#C4614B" strokeWidth="9" strokeLinecap="round" strokeLinejoin="round">
          <path d="M96 300V78M196 300V78" /><path d="M96 150h100M96 220h100" />
        </g>
        <g stroke="#C4614B" strokeWidth="5" strokeLinecap="round"><path d="M96 150l100 70M196 150l-100 70M146 78V40M146 40L-60 74M146 40L236 74" /></g>
        <rect x="-70" y="70" width="316" height="14" rx="4" fill="#C4614B" stroke="#7A3526" strokeWidth="3" />
        <rect x="150" y="50" width="42" height="24" rx="4" fill="#EEF4F6" stroke="#6B8794" strokeWidth="3" />
      </g>

      <g id="crane-trolley">
        <rect x="-34" y="82" width="36" height="12" rx="3" fill="#23485A" />
        <motion.g
          id="crane-hoist"
          style={{ transformBox: 'fill-box', transformOrigin: '50% 0%' }}
          initial={false}
          {...anim(playing, { scaleY: hoist.value }, { duration: CYCLE, times: hoist.times, ease: 'easeInOut' }, { scaleY: 1 })}
        >
          <path d="M-26 94V168M-6 94V168" stroke="#44626E" strokeWidth="3" />
          <rect x="-46" y="168" width="60" height="9" rx="3" fill="#23485A" />
        </motion.g>
      </g>
    </g>
  );
}

function Ship({ playing }) {
  return (
    <g transform={`translate(${SHIP_TX} ${SHIP_TY}) scale(0.3)`}>
      {/* Wake trails behind the stern (left), only while sailing. */}
      <motion.g
        initial={false}
        {...anim(playing, { opacity: wakeEnvelope.value }, { duration: CYCLE, times: wakeEnvelope.times, ease: 'linear' }, { opacity: 0 })}
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

      {/* The ship art is authored facing left and mirrored to face right. */}
      <g transform="translate(860 0) scale(-1 1)">
        <motion.g
          id="ship-beacon"
          style={{ transformBox: 'fill-box', transformOrigin: 'center' }}
          initial={false}
          {...anim(playing, { scale: [1, 1.3, 1], opacity: [1, 0.4, 1] }, { duration: 2.4, ease: 'easeInOut' }, { scale: 1, opacity: 1 })}
        >
          <circle cx="750" cy="46" r="20" stroke="#B9D3DB" strokeWidth="2" strokeDasharray="3 5" />
          <circle cx="750" cy="46" r="12" stroke="#B9D3DB" strokeWidth="2" />
          <circle cx="750" cy="46" r="6" fill="#D9694F" />
        </motion.g>

        <motion.g
          id="ship-smoke" fill="#C9DCE2"
          initial={false}
          {...anim(playing, { y: [0, -14, -24], opacity: [0.85, 0.5, 0] }, { duration: 3, ease: 'easeOut' }, { opacity: 0 })}
        >
          <circle cx="806" cy="82" r="8" /><circle cx="812" cy="60" r="11" opacity="0.7" /><circle cx="822" cy="36" r="14" opacity="0.4" />
        </motion.g>

        <ShipBody />
      </g>
    </g>
  );
}

// ── Component ────────────────────────────────────────────────────────────────
export default function BannerScene() {
  const ref = useRef(null);
  const reduce = useReducedMotion();
  const [mode, setMode] = useState('full');     // 'full' | 'compact' | 'sea'
  const [inView, setInView] = useState(true);
  const [tabVisible, setTabVisible] = useState(true);

  // Responsive mode from the banner's own content width, matching the
  // @container thresholds in dashboard.css: <640 sea only, <1000 compact, else full.
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

  // Pause when off-screen or when the tab is hidden.
  useEffect(() => {
    const el = ref.current;
    if (!el) return undefined;
    const io = new IntersectionObserver(([entry]) => setInView(entry.isIntersecting), { threshold: 0 });
    io.observe(el);
    const onVis = () => setTabVisible(!document.hidden);
    document.addEventListener('visibilitychange', onVis);
    return () => { io.disconnect(); document.removeEventListener('visibilitychange', onVis); };
  }, []);

  // The sea height and the stage size are set in dashboard.css from one shared
  // value (--stage-h), so the waterline cannot drift. No JS sizing here.

  const visible = inView && tabVisible && !reduce;
  const seaPlaying = visible;                          // water drifts in every mode
  const playing = mode === 'full' && visible;          // ship + dock animate in full only

  return (
    <div className="dash-banner__scene" ref={ref} aria-hidden="true">
      {/* Full-width sea + waves */}
      <svg className="dash-banner__sea" viewBox="0 0 1000 60" preserveAspectRatio="none" focusable="false">
        <rect x="0" y="0" width="1000" height="60" fill="#D3E7EE" />
        <motion.g
          initial={false}
          {...anim(seaPlaying, { x: [0, -100] }, { duration: 7, ease: 'linear' }, { x: 0 })}
          stroke="#9CC4D0" fill="none" strokeLinecap="round"
        >
          {WAVES.map((w) => (
            <g key={w.y} transform={`translate(${w.phase} 0)`}>
              <path d={seaWave(w.y, w.amp)} strokeWidth="2.5" opacity={w.opacity} />
            </g>
          ))}
        </motion.g>
      </svg>

      {/* Dock + ship stage (hidden on phone-width banners) */}
      {mode !== 'sea' && (
        <svg
          className="dash-banner__stage"
          viewBox="0 0 900 260"
          preserveAspectRatio="xMaxYMax meet"
          focusable="false"
        >
          {mode === 'full' && <DockArt playing={playing} />}
          <motion.g
            initial={false}
            animate={playing ? { x: voyage.x.value, opacity: voyage.opacity.value } : { x: 0, opacity: 1 }}
            transition={playing ? {
              x: { duration: CYCLE, times: voyage.x.times, ease: voyage.x.ease, repeat: Infinity },
              opacity: { duration: CYCLE, times: voyage.opacity.times, ease: voyage.opacity.ease, repeat: Infinity },
            } : { duration: 0 }}
          >
            <motion.g
              style={{ transformBox: 'fill-box', transformOrigin: '50% 80%' }}
              initial={false}
              {...anim(playing, { y: [0, -2.5, 0], rotate: [-0.6, 0.6, -0.6] }, { duration: 4, ease: 'easeInOut' }, { y: 0, rotate: 0 })}
            >
              <Ship playing={playing} />
            </motion.g>
          </motion.g>
        </svg>
      )}
    </div>
  );
}