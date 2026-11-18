import React from 'react';

export default function Card({ title, actions, children, className = '', padding = true }) {
  return (
    <div className={`rounded-xl bg-white shadow-sm ring-1 ring-ink/5 ${className}`}>
      {(title || actions) && (
        <div className="flex items-center justify-between border-b border-ink/10 px-5 py-3">
          {title && <h3 className="m-0 text-base font-semibold text-ink">{title}</h3>}
          {actions && <div className="flex items-center gap-2">{actions}</div>}
        </div>
      )}
      <div className={padding ? 'px-5 py-4' : ''}>{children}</div>
    </div>
  );
}
