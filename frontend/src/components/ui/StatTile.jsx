import { motion, useReducedMotion } from 'motion/react';
import { cardEnter, noMotion } from './motionPresets';

// StatTile — a single KPI tile: icon, value, label, and optional hint.
export default function StatTile({
  icon: Icon,
  value,
  label,
  hint,
  tone = 'primary',
  className = '',
}) {
  const reduce = useReducedMotion();
  const preset = reduce ? noMotion : cardEnter;

  return (
    <motion.div
      className={`ui-stat ui-stat--${tone} ${className}`}
      initial={preset.initial}
      animate={preset.animate}
      transition={preset.transition}
    >
      {Icon && (
        <span className="ui-stat__icon" aria-hidden="true">
          <Icon />
        </span>
      )}
      <div className="ui-stat__body">
        <div className="ui-stat__value">{value}</div>
        <div className="ui-stat__label">{label}</div>
        {hint && <div className="ui-stat__hint">{hint}</div>}
      </div>
    </motion.div>
  );
}
