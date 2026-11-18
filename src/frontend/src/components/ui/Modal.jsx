import React, { useEffect } from 'react';

const SIZES = {
  sm: 'max-w-sm',
  md: 'max-w-lg',
  lg: 'max-w-2xl',
  xl: 'max-w-4xl'
};

export default function Modal({ open, onClose, title, children, footer, size = 'md' }) {
  useEffect(() => {
    if (!open) return undefined;
    const onKey = (e) => {
      if (e.key === 'Escape') onClose?.();
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-[1000] flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      aria-label={typeof title === 'string' ? title : undefined}
    >
      <div className="absolute inset-0 bg-black/50" onClick={onClose} aria-hidden="true" />
      <div className={`relative w-full ${SIZES[size] || SIZES.md} rounded-2xl bg-white shadow-xl`}>
        <div className="flex items-center justify-between border-b border-ink/10 px-5 py-3">
          <h3 className="m-0 text-lg font-semibold text-ink">{title}</h3>
          <button
            type="button"
            onClick={onClose}
            aria-label="Закрити"
            className="rounded-lg p-1 text-2xl leading-none text-ink-light transition-colors hover:bg-ink/5 hover:text-ink"
          >
            &times;
          </button>
        </div>
        <div className="px-5 py-4">{children}</div>
        {footer && <div className="flex justify-end gap-3 border-t border-ink/10 px-5 py-3">{footer}</div>}
      </div>
    </div>
  );
}
