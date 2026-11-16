import { motion, useReducedMotion } from 'motion/react';
import { cardEnter, noMotion } from '../ui/motionPresets';
import BannerScene from './BannerScene';

// Banner — pale blue command-center panel with an animated harbour scene on the
// right. `text` is the one line of supporting copy, which differs per portal.
export default function Banner({ text }) {
  const reduce = useReducedMotion();
  const preset = reduce ? noMotion : cardEnter;

  return (
    <motion.div
      className="dash-banner"
      initial={preset.initial}
      animate={preset.animate}
      transition={preset.transition}
    >
      <BannerScene />
      <div className="dash-banner__content">
        <p className="dash-banner__eyebrow">Workspace command center</p>
        <h2 className="dash-banner__title">
          Everything important,<br />
          <span>in one clear view.</span>
        </h2>
        {text && <p className="dash-banner__text">{text}</p>}
      </div>
    </motion.div>
  );
}
