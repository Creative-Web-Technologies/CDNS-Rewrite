import { motion, useReducedMotion } from 'motion/react';
import { pageEnter, noMotion } from './motionPresets';

// PageTransition — wraps route content so it animates in with page-enter.
// Respects prefers-reduced-motion.
export default function PageTransition({ children, className = '' }) {
  const reduce = useReducedMotion();
  const preset = reduce ? noMotion : pageEnter;

  return (
    <motion.div
      className={className}
      initial={preset.initial}
      animate={preset.animate}
      transition={preset.transition}
    >
      {children}
    </motion.div>
  );
}
