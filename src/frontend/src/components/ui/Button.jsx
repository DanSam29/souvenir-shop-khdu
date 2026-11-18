import React from 'react';

const VARIANTS = {
  primary: 'bg-brand text-white hover:bg-brand-dark focus-visible:ring-brand',
  secondary: 'bg-white text-brand border border-brand hover:bg-brand/5 focus-visible:ring-brand',
  danger: 'bg-red-600 text-white hover:bg-red-700 focus-visible:ring-red-500',
  outlined: 'bg-transparent text-ink border border-ink/30 hover:bg-ink/5 focus-visible:ring-ink',
  ghost: 'bg-transparent text-ink hover:bg-ink/5 focus-visible:ring-ink',
  gold: 'bg-gold text-ink hover:bg-gold-dark focus-visible:ring-gold'
};

const SIZES = {
  sm: 'px-3 py-1.5 text-sm',
  md: 'px-4 py-2 text-sm',
  lg: 'px-6 py-3 text-base'
};

const Button = React.forwardRef(function Button(
  {
    variant = 'primary',
    size = 'md',
    loading = false,
    disabled = false,
    type = 'button',
    className = '',
    children,
    ...props
  },
  ref
) {
  const isDisabled = disabled || loading;
  return (
    <button
      ref={ref}
      type={type}
      disabled={isDisabled}
      aria-busy={loading || undefined}
      className={[
        'inline-flex items-center justify-center gap-2 rounded-lg font-semibold',
        'transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2',
        'disabled:opacity-60 disabled:cursor-not-allowed',
        VARIANTS[variant] || VARIANTS.primary,
        SIZES[size] || SIZES.md,
        className
      ].join(' ')}
      {...props}
    >
      {loading && (
        <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
        </svg>
      )}
      {children}
    </button>
  );
});

export default Button;
