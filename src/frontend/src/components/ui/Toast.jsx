import React, { createContext, useCallback, useContext, useState } from 'react';

const ToastContext = createContext(null);

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    return { showToast: () => {} };
  }
  return ctx;
}

const TONES = {
  success: 'bg-green-50 border-green-300 text-green-900',
  error: 'bg-red-50 border-red-300 text-red-900',
  warning: 'bg-amber-50 border-amber-300 text-amber-900',
  info: 'bg-blue-50 border-blue-300 text-blue-900'
};

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);

  const dismiss = useCallback((id) => setToasts((prev) => prev.filter((t) => t.id !== id)), []);

  const showToast = useCallback((message, type = 'info', duration = 4000) => {
    if (!message) return;
    const id = `${Date.now()}-${Math.random()}`;
    setToasts((prev) => [...prev, { id, message, type }]);
    setTimeout(() => dismiss(id), duration);
  }, [dismiss]);

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="fixed right-5 top-5 z-[2000] flex max-w-[360px] flex-col gap-2.5" aria-live="polite">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            role="alert"
            onClick={() => dismiss(toast.id)}
            className={[
              'cursor-pointer rounded-lg border px-4 py-3 text-[0.95rem] shadow-lg',
              TONES[toast.type] || TONES.info
            ].join(' ')}
          >
            {toast.message}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
