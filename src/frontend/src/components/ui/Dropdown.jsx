import React, { useEffect, useRef, useState } from 'react';

export default function Dropdown({ label, items = [], align = 'right', buttonClassName = '' }) {
  const [open, setOpen] = useState(false);
  const ref = useRef(null);

  useEffect(() => {
    const onClick = (e) => {
      if (ref.current && !ref.current.contains(e.target)) setOpen(false);
    };
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, []);

  return (
    <div className="relative inline-block" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="menu"
        aria-expanded={open}
        className={[
          'inline-flex items-center gap-2 rounded-lg border border-ink/20 bg-white px-4 py-2 text-sm font-medium text-ink hover:bg-ink/5 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand/40',
          buttonClassName
        ].join(' ')}
      >
        {label}
        <span aria-hidden="true" className="text-xs">
          ▾
        </span>
      </button>
      {open && (
        <ul
          role="menu"
          className={`absolute z-50 mt-2 min-w-[180px] rounded-lg border border-ink/10 bg-white py-1 shadow-lg ${
            align === 'left' ? 'left-0' : 'right-0'
          }`}
        >
          {items.map((item, idx) => (
            <li key={item.key ?? idx} role="menuitem">
              <button
                type="button"
                disabled={item.disabled}
                onClick={() => {
                  setOpen(false);
                  item.onClick?.();
                }}
                className="w-full px-4 py-2 text-left text-sm text-ink transition-colors hover:bg-brand/5 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {item.label}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
