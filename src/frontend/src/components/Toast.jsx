import React, { createContext, useCallback, useContext, useState } from 'react';

const ToastContext = createContext(null);

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    return { showToast: () => {} };
  }
  return ctx;
}

const COLORS = {
  success: { background: '#f6ffed', border: '1px solid #b7eb8f', color: '#1e4620' },
  error: { background: '#fff1f0', border: '1px solid #ffa39e', color: '#a8071a' },
  info: { background: '#e6f7ff', border: '1px solid #91d5ff', color: '#003a8c' }
};

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);

  const showToast = useCallback((message, type = 'info', duration = 4000) => {
    if (!message) return;
    const id = `${Date.now()}-${Math.random()}`;
    setToasts((prev) => [...prev, { id, message, type }]);
    setTimeout(() => {
      setToasts((prev) => prev.filter((toast) => toast.id !== id));
    }, duration);
  }, []);

  const dismiss = (id) => setToasts((prev) => prev.filter((toast) => toast.id !== id));

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div
        style={{
          position: 'fixed',
          top: 20,
          right: 20,
          zIndex: 2000,
          display: 'flex',
          flexDirection: 'column',
          gap: 10,
          maxWidth: 360
        }}
      >
        {toasts.map((toast) => {
          const palette = COLORS[toast.type] || COLORS.info;
          return (
            <div
              key={toast.id}
              role="alert"
              onClick={() => dismiss(toast.id)}
              style={{
                ...palette,
                padding: '12px 16px',
                borderRadius: 8,
                boxShadow: '0 4px 12px rgba(0,0,0,0.12)',
                fontSize: '0.95rem',
                cursor: 'pointer'
              }}
            >
              {toast.message}
            </div>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}
