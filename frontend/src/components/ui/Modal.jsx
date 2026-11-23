import { useEffect, useId, useRef } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import { AnimatePresence, motion, useReducedMotion } from 'motion/react';
import './modal.css';

const SIZES = { sm: 480, md: 720, lg: 960 };

export default function Modal({
  open,
  onClose,
  title,
  subtitle,
  size = 'md',
  tabs,
  footer,
  children,
}) {
  const containerRef = useRef(null);
  const triggerRef = useRef(null);
  const shouldReduce = useReducedMotion();
  const uid = useId();
  const titleId = `${uid}-title`;
  const descId = `${uid}-desc`;

  // Capture the trigger element when the modal opens
  useEffect(() => {
    if (open) {
      triggerRef.current = document.activeElement;
    }
  }, [open]);

  // Focus first focusable element on open
  useEffect(() => {
    if (!open) return;
    const t = requestAnimationFrame(() => {
      const first = containerRef.current?.querySelector(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      );
      first?.focus();
    });
    return () => cancelAnimationFrame(t);
  }, [open]);

  // Return focus to trigger on close
  useEffect(() => {
    if (!open && triggerRef.current) {
      triggerRef.current?.focus();
      triggerRef.current = null;
    }
  }, [open]);

  // Body scroll lock
  useEffect(() => {
    if (!open) return;
    const prev = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => { document.body.style.overflow = prev; };
  }, [open]);

  // Esc key + focus trap
  useEffect(() => {
    if (!open) return;
    function onKeyDown(e) {
      if (e.key === 'Escape') { onClose(); return; }
      if (e.key !== 'Tab') return;
      const focusable = Array.from(
        containerRef.current?.querySelectorAll(
          'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
        ) ?? [],
      );
      if (focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (e.shiftKey) {
        if (document.activeElement === first) { e.preventDefault(); last.focus(); }
      } else {
        if (document.activeElement === last) { e.preventDefault(); first.focus(); }
      }
    }
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [open, onClose]);

  const maxWidth = SIZES[size] ?? SIZES.md;

  const backdropVariants = shouldReduce
    ? { initial: { opacity: 0 }, animate: { opacity: 1 }, exit: { opacity: 0 }, transition: { duration: 0.08 } }
    : { initial: { opacity: 0 }, animate: { opacity: 1 }, exit: { opacity: 0 }, transition: { duration: 0.16 } };

  const dialogVariants = shouldReduce
    ? { initial: { opacity: 0 }, animate: { opacity: 1 }, exit: { opacity: 0 }, transition: { duration: 0.08 } }
    : {
        initial: { opacity: 0, scale: 0.98 },
        animate: { opacity: 1, scale: 1 },
        exit: { opacity: 0, scale: 0.98 },
        transition: { duration: 0.16, ease: [0.16, 1, 0.3, 1] },
      };

  return createPortal(
    <AnimatePresence>
      {open && (
        <motion.div
          className="ui-modal-backdrop"
          onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
          {...backdropVariants}
        >
          <motion.div
            ref={containerRef}
            role="dialog"
            aria-modal="true"
            aria-labelledby={title ? titleId : undefined}
            aria-describedby={subtitle ? descId : undefined}
            className="ui-modal"
            style={{ maxWidth }}
            {...dialogVariants}
          >
            <div className="ui-modal__header">
              <div className="ui-modal__header-text">
                {title && <h2 id={titleId} className="ui-modal__title">{title}</h2>}
                {subtitle && <p id={descId} className="ui-modal__subtitle">{subtitle}</p>}
              </div>
              <button
                type="button"
                className="ui-modal__close"
                aria-label="Close"
                onClick={onClose}
              >
                <X size={18} aria-hidden="true" />
              </button>
            </div>

            {tabs && <div className="ui-modal__tabs">{tabs}</div>}

            <div className="ui-modal__body">{children}</div>

            {footer && <div className="ui-modal__footer">{footer}</div>}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>,
    document.body,
  );
}
