import React from 'react';
import { useTranslation } from 'react-i18next';

const SIZES = {
  sm: 'h-4 w-4',
  md: 'h-6 w-6',
  lg: 'h-10 w-10'
};

export default function Spinner({ size = 'md', className = '' }) {
  const { t } = useTranslation();
  return (
    <svg
      className={`inline-block animate-spin text-brand ${SIZES[size] || SIZES.md} ${className}`}
      viewBox="0 0 24 24"
      fill="none"
      role="status"
      aria-label={t('common.loading')}
    >
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
    </svg>
  );
}

const TONES = {
  brand: 'bg-brand',
  success: 'bg-green-500',
  warning: 'bg-amber-500',
  danger: 'bg-red-500'
};

export function ProgressBar({ value = 0, max = 100, tone = 'brand', label = null, className = '' }) {
  const pct = Math.max(0, Math.min(100, (value / max) * 100));
  return (
    <div className={`w-full ${className}`}>
      {label && (
        <div className="mb-1 flex justify-between text-xs text-ink-light">
          <span>{label}</span>
          <span>{Math.round(pct)}%</span>
        </div>
      )}
      <div
        className="h-2 w-full overflow-hidden rounded-full bg-ink/10"
        role="progressbar"
        aria-valuenow={value}
        aria-valuemin={0}
        aria-valuemax={max}
      >
        <div className={`h-full rounded-full transition-all ${TONES[tone] || TONES.brand}`} style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}
