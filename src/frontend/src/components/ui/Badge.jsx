import React from 'react';

const TONES = {
  default: 'bg-ink/10 text-ink',
  brand: 'bg-brand/10 text-brand',
  success: 'bg-green-100 text-green-800',
  danger: 'bg-red-100 text-red-700',
  warning: 'bg-amber-100 text-amber-800',
  info: 'bg-blue-100 text-blue-700'
};

export default function Badge({ tone = 'default', children, className = '' }) {
  return (
    <span
      className={[
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
        TONES[tone] || TONES.default,
        className
      ].join(' ')}
    >
      {children}
    </span>
  );
}
