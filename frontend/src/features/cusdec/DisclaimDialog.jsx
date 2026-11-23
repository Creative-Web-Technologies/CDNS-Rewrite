import { useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { AlertTriangle } from 'lucide-react';
import { Button } from '../../components/ui';

export default function DisclaimDialog({ item, busy, onConfirm, onCancel }) {
  const dialogRef = useRef(null);
  const triggerRef = useRef(null);

  // Focus the dialog on open; store the element that opened it so we can
  // return focus on close.
  useEffect(() => {
    triggerRef.current = document.activeElement;
    // Focus the first interactive element (the "Keep it" button)
    const firstBtn = dialogRef.current?.querySelector('button');
    firstBtn?.focus();

    return () => {
      triggerRef.current?.focus();
    };
  }, []);

  // Trap focus inside the dialog and close on Esc
  useEffect(() => {
    function onKeyDown(e) {
      if (e.key === 'Escape') { onCancel(); return; }
      if (e.key !== 'Tab') return;

      const focusable = dialogRef.current?.querySelectorAll(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])',
      );
      if (!focusable || focusable.length === 0) return;

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
  }, [onCancel]);

  const number = item?.cusDecNumber ?? 'this declaration';

  return createPortal(
    <div className="cd-dialog-backdrop" aria-hidden="false" onClick={(e) => { if (e.target === e.currentTarget) onCancel(); }}>
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="disclaim-title"
        aria-describedby="disclaim-desc"
        className="cd-dialog"
      >
        <div className="cd-dialog__icon" aria-hidden="true">
          <AlertTriangle size={22} />
        </div>
        <h2 id="disclaim-title" className="cd-dialog__title">Disclaim {number}?</h2>
        <p id="disclaim-desc" className="cd-dialog__body">
          You are confirming this CusDec does not belong to your company. It moves to Disclaimed and can't be moved back.
        </p>
        <div className="cd-dialog__actions">
          <Button variant="secondary" onClick={onCancel} disabled={busy}>
            Keep it
          </Button>
          <Button
            variant="danger"
            onClick={onConfirm}
            disabled={busy}
            aria-busy={busy}
          >
            {busy ? 'Disclaiming…' : 'Disclaim'}
          </Button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
