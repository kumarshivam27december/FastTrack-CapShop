import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import './Toast.css';

const ToastContext = createContext(null);

let idCounter = 1;

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);

  const remove = useCallback((id) => {
    setToasts((t) => t.filter((x) => x.id !== id));
  }, []);

  const show = useCallback((message, type = 'info', ttl = 3500) => {
    const id = idCounter++;
    setToasts((t) => [...t, { id, message, type }]);
    if (ttl > 0) setTimeout(() => remove(id), ttl);
    return id;
  }, [remove]);

  useEffect(() => {
    return () => {
      // cleanup timeouts when unmounting is fine — nothing to do here
    };
  }, []);

  const api = {
    show,
    success: (msg, ttl) => show(msg, 'success', ttl),
    error: (msg, ttl) => show(msg, 'error', ttl),
    info: (msg, ttl) => show(msg, 'info', ttl)
  };

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="toast-viewport" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast toast-${t.type}`} role="status">
            {t.message}
            <button className="toast-close" onClick={() => remove(t.id)} aria-label="Dismiss">×</button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return ctx;
}

export default ToastProvider;
