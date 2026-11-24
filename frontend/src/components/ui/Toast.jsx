import { createContext, useCallback, useContext, useState } from 'react';
import { createPortal } from 'react-dom';
import { CheckCircle2, XCircle, X } from 'lucide-react';
import { motion, AnimatePresence, useReducedMotion } from 'motion/react';
import './toast.css';

const ToastContext = createContext({ addToast: () => {} });

export function useToast() {
  return useContext(ToastContext);
}

let _nextId = 1;

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);

  const dismiss = useCallback((id) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const addToast = useCallback(({ type = 'success', message }) => {
    const id = _nextId++;
    setToasts((prev) => [...prev, { id, type, message }].slice(-3));
    const delay = type === 'error' ? 6000 : 4000;
    const timer = setTimeout(() => dismiss(id), delay);
    return () => clearTimeout(timer);
  }, [dismiss]);

  return (
    <ToastContext.Provider value={{ addToast }}>
      {children}
      {createPortal(
        <ToastStack toasts={toasts} onDismiss={dismiss} />,
        document.body,
      )}
    </ToastContext.Provider>
  );
}

function ToastStack({ toasts, onDismiss }) {
  const reduce = useReducedMotion();

  return (
    <div className="toast-stack" aria-live="polite" aria-atomic="false">
      <AnimatePresence>
        {toasts.map((toast) => (
          <motion.div
            key={toast.id}
            className={`toast toast--${toast.type}`}
            role="status"
            initial={reduce ? { opacity: 1 } : { opacity: 0, y: 20, scale: 0.96 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={reduce ? { opacity: 0 } : { opacity: 0, y: -6, scale: 0.98 }}
            transition={{ duration: 0.22, ease: [0.16, 1, 0.3, 1] }}
          >
            <span className="toast__icon" aria-hidden="true">
              {toast.type === 'success'
                ? <CheckCircle2 size={16} />
                : <XCircle size={16} />}
            </span>
            <span className="toast__message">{toast.message}</span>
            <button
              type="button"
              className="toast__dismiss"
              aria-label="Dismiss notification"
              onClick={() => onDismiss(toast.id)}
            >
              <X size={13} aria-hidden="true" />
            </button>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  );
}
