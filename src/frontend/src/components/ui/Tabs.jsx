import React, { useState } from 'react';

export function Tabs({ items = [], active, onChange, className = '' }) {
  const current = active ?? items[0]?.key;
  const activeItem = items.find((it) => it.key === current);
  return (
    <div className={className}>
      <div role="tablist" className="flex flex-wrap gap-1 border-b border-ink/10">
        {items.map((item) => {
          const isActive = item.key === current;
          return (
            <button
              key={item.key}
              type="button"
              role="tab"
              aria-selected={isActive}
              onClick={() => onChange?.(item.key)}
              className={[
                '-mb-px border-b-2 px-4 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'border-brand text-brand'
                  : 'border-transparent text-ink-light hover:border-ink/20 hover:text-ink'
              ].join(' ')}
            >
              {item.label}
            </button>
          );
        })}
      </div>
      {activeItem?.content !== undefined && (
        <div role="tabpanel" className="pt-4">
          {activeItem.content}
        </div>
      )}
    </div>
  );
}

export function Accordion({ items = [], className = '' }) {
  const [openIndex, setOpenIndex] = useState(null);
  return (
    <div className={`divide-y divide-ink/10 rounded-xl border border-ink/10 bg-white ${className}`}>
      {items.map((item, idx) => {
        const isOpen = openIndex === idx;
        return (
          <div key={item.key ?? idx}>
            <button
              type="button"
              aria-expanded={isOpen}
              onClick={() => setOpenIndex(isOpen ? null : idx)}
              className="flex w-full items-center justify-between px-4 py-3 text-left text-sm font-medium text-ink hover:bg-ink/5"
            >
              {item.title}
              <span aria-hidden="true" className="text-xs">
                {isOpen ? '▲' : '▼'}
              </span>
            </button>
            {isOpen && <div className="px-4 pb-3 text-sm text-ink-light">{item.content}</div>}
          </div>
        );
      })}
    </div>
  );
}
