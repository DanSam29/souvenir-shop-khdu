import React from 'react';

const BASE = 'w-full rounded-lg border bg-white px-3 py-2 text-sm text-ink placeholder:text-ink-light focus:outline-none focus:ring-2 focus:ring-brand/40 focus:border-brand disabled:bg-gray-100 disabled:cursor-not-allowed';
const NORMAL = 'border-ink/20';
const INVALID = 'border-red-500 focus:ring-red-300 focus:border-red-500';

function Field({ label, htmlFor, error, errorId, required, className = '', children }) {
  return (
    <div className={`flex flex-col gap-1 ${className}`}>
      {label && (
        <label htmlFor={htmlFor} className="text-sm font-medium text-ink">
          {label}
          {required && <span className="text-red-500"> *</span>}
        </label>
      )}
      {children}
      {error && (
        <span id={errorId} className="text-xs text-red-600">
          {error}
        </span>
      )}
    </div>
  );
}

export function Input({ label, error, id, className = '', required, ...props }) {
  const autoId = React.useId();
  const inputId = id || `input-${autoId}`;
  const errorId = error ? `${inputId}-error` : undefined;
  return (
    <Field label={label} htmlFor={inputId} error={error} errorId={errorId} required={required}>
      <input
        id={inputId}
        required={required}
        aria-invalid={error ? true : undefined}
        aria-describedby={errorId}
        className={[BASE, error ? INVALID : NORMAL, className].join(' ')}
        {...props}
      />
    </Field>
  );
}

export function Textarea({ label, error, id, className = '', required, rows = 4, ...props }) {
  const autoId = React.useId();
  const inputId = id || `textarea-${autoId}`;
  const errorId = error ? `${inputId}-error` : undefined;
  return (
    <Field label={label} htmlFor={inputId} error={error} errorId={errorId} required={required}>
      <textarea
        id={inputId}
        rows={rows}
        required={required}
        aria-invalid={error ? true : undefined}
        aria-describedby={errorId}
        className={[BASE, 'resize-y', error ? INVALID : NORMAL, className].join(' ')}
        {...props}
      />
    </Field>
  );
}

export function Select({ label, error, id, className = '', required, children, ...props }) {
  const autoId = React.useId();
  const inputId = id || `select-${autoId}`;
  const errorId = error ? `${inputId}-error` : undefined;
  return (
    <Field label={label} htmlFor={inputId} error={error} errorId={errorId} required={required}>
      <select
        id={inputId}
        required={required}
        aria-invalid={error ? true : undefined}
        aria-describedby={errorId}
        className={[BASE, error ? INVALID : NORMAL, className].join(' ')}
        {...props}
      >
        {children}
      </select>
    </Field>
  );
}

export function Checkbox({ label, id, className = '', ...props }) {
  const autoId = React.useId();
  const inputId = id || `checkbox-${autoId}`;
  return (
    <label htmlFor={inputId} className={`inline-flex items-center gap-2 text-sm text-ink cursor-pointer ${className}`}>
      <input
        id={inputId}
        type="checkbox"
        className="h-4 w-4 rounded border-ink/30 text-brand focus:ring-2 focus:ring-brand/40 disabled:cursor-not-allowed"
        {...props}
      />
      {label}
    </label>
  );
}

export function Radio({ label, id, name, className = '', ...props }) {
  const autoId = React.useId();
  const inputId = id || `radio-${autoId}`;
  return (
    <label htmlFor={inputId} className={`inline-flex items-center gap-2 text-sm text-ink cursor-pointer ${className}`}>
      <input
        id={inputId}
        type="radio"
        name={name}
        className="h-4 w-4 border-ink/30 text-brand focus:ring-2 focus:ring-brand/40 disabled:cursor-not-allowed"
        {...props}
      />
      {label}
    </label>
  );
}
