import type { ButtonHTMLAttributes, ReactNode } from 'react'

// ── Variant and size contracts ────────────────────────────────────────────────

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'
type Size    = 'sm' | 'md' | 'lg'

const VARIANT_CLASSES: Record<Variant, string> = {
  primary:
    'bg-brand-600 text-white hover:bg-brand-700 ' +
    'focus-visible:ring-brand-500',
  secondary:
    'bg-surface border border-stroke text-content-primary hover:bg-surface-raised ' +
    'focus-visible:ring-brand-500',
  ghost:
    'text-content-secondary hover:bg-surface-overlay ' +
    'focus-visible:ring-brand-500',
  danger:
    'bg-error text-white hover:bg-error-dark focus-visible:ring-error',
}

const SIZE_CLASSES: Record<Size, string> = {
  sm: 'px-3 py-1.5 text-xs',
  md: 'px-4 py-2 text-sm',
  lg: 'px-5 py-2.5 text-base',
}

// ── Props ─────────────────────────────────────────────────────────────────────

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?:   Variant
  size?:      Size
  isLoading?: boolean
  children:   ReactNode
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * Accessible button primitive.
 *
 * - Uses `focus-visible:ring` (not `focus:outline-none`) so keyboard users
 *   always see a visible focus ring.
 * - Sets `aria-busy` while loading so screen readers announce the state.
 * - Defaults to `type="button"` to avoid accidental form submission.
 *
 * Usage:
 *   <Button variant="secondary" size="sm" onClick={save}>Save draft</Button>
 *   <Button isLoading>Submitting…</Button>
 */
export function Button({
  variant   = 'primary',
  size      = 'md',
  isLoading = false,
  disabled,
  className = '',
  children,
  ...props
}: ButtonProps) {
  const isDisabled = disabled || isLoading

  return (
    <button
      type="button"
      aria-busy={isLoading || undefined}
      disabled={isDisabled}
      className={[
        'inline-flex items-center justify-center gap-2 font-medium rounded-md',
        'transition-colors duration-150',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2',
        'disabled:opacity-50 disabled:cursor-not-allowed',
        VARIANT_CLASSES[variant],
        SIZE_CLASSES[size],
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      {...props}
    >
      {isLoading && (
        <svg
          className="animate-spin -ms-0.5 h-4 w-4"
          aria-hidden="true"
          fill="none"
          viewBox="0 0 24 24"
        >
          <circle
            className="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            strokeWidth="4"
          />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
          />
        </svg>
      )}
      {children}
    </button>
  )
}
