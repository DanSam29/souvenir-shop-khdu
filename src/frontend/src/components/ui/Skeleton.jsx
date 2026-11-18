import React from 'react';

const BASE = 'animate-pulse rounded bg-ink/10';

export function Skeleton({ className = '', ...props }) {
  return <div aria-hidden="true" className={`${BASE} ${className}`} {...props} />;
}

export function SkeletonText({ lines = 3, className = '' }) {
  return (
    <div aria-hidden="true" className={`flex flex-col gap-2 ${className}`}>
      {Array.from({ length: lines }).map((_, i) => (
        <div
          key={i}
          className={`${BASE} h-3`}
          style={{ width: i === lines - 1 ? '60%' : '100%' }}
        />
      ))}
    </div>
  );
}

export function ProductCardSkeleton() {
  return (
    <div aria-hidden="true" className="flex flex-col gap-3 rounded-xl bg-white p-4 shadow">
      <div className={`${BASE} h-48 w-full`} />
      <div className={`${BASE} h-4 w-3/4`} />
      <div className={`${BASE} h-4 w-1/2`} />
      <div className={`${BASE} h-9 w-full`} />
    </div>
  );
}

export function TableSkeleton({ rows = 5, cols = 4 }) {
  return (
    <div aria-hidden="true" className="flex w-full flex-col gap-2">
      {Array.from({ length: rows }).map((_, r) => (
        <div
          key={r}
          className="grid gap-3"
          style={{ gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))` }}
        >
          {Array.from({ length: cols }).map((_, c) => (
            <div key={c} className={`${BASE} h-6`} />
          ))}
        </div>
      ))}
    </div>
  );
}
