import { motion, useReducedMotion } from 'motion/react';
import { cardEnter, noMotion } from './motionPresets';

// Card — a surface container with an optional header (title + actions).
// Animates in with card-enter unless the user prefers reduced motion.
export default function Card({
  eyebrow,
  title,
  subtitle,
  actions,
  padding = true,
  className = '',
  children,
  ...rest
}) {
  const reduce = useReducedMotion();
  const preset = reduce ? noMotion : cardEnter;

  return (
    <motion.section
      className={`ui-card ${className}`}
      initial={preset.initial}
      animate={preset.animate}
      transition={preset.transition}
      {...rest}
    >
      {(eyebrow || title || actions) && (
        <header className="ui-card__header">
          <div>
            {eyebrow && <span className="ui-card__eyebrow">{eyebrow}</span>}
            {title && <h2 className="ui-card__title">{title}</h2>}
            {subtitle && <p className="ui-card__subtitle">{subtitle}</p>}
          </div>
          {actions && <div className="ui-card__actions">{actions}</div>}
        </header>
      )}
      <div className={padding ? 'ui-card__body' : 'ui-card__body ui-card__body--flush'}>
        {children}
      </div>
    </motion.section>
  );
}
