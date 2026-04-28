import { useId, type InputHTMLAttributes } from 'react'

// ── Props ─────────────────────────────────────────────────────────────────────

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  /** Visible label — required. Never hide the label; use `sr-only` if needed. */
  label:       string
  /** Supporting text shown below the input when there is no error. */
  helperText?: string
  /**
   * Error message. When set:
   * - The field renders with an error border colour
   * - The message appears below the field in an `role="alert"` paragraph
   * - `aria-invalid` is set on the <input>
   */
  error?:      string
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * Accessible text input primitive.
 *
 * Accessibility:
 * - Label is always visible; linked to the input via `htmlFor` + `id`
 * - `aria-describedby` wires helper/error text to the input for screen readers
 * - `aria-invalid` set when an error is present
 * - Required fields show a visual asterisk (aria-hidden) + native `required`
 * - Focus ring uses `focus-visible:ring` so mouse users aren't distracted
 *
 * RTL: no directional classes — text inside an <input> direction follows
 * the document `dir` attribute automatically.
 *
 * Tenant theming: border, background, and ring colours resolve from CSS vars.
 *
 * Usage:
 *   <Input label="Email address" type="email" required />
 *   <Input label="Username" error="Username is already taken" />
 *   <Input label="Bio" helperText="Max 160 characters" />
 */
export function Input({
  label,
  helperText,
  error,
  id:        idProp,
  required,
  className = '',
  ...props
}: InputProps) {
  const generatedId = useId()
  const id          = idProp ?? generatedId
  const helperId    = `${id}-helper`
  const errorId     = `${id}-error`
  const hasError    = Boolean(error)

  const describedBy = [hasError && errorId, !hasError && helperText && helperId]
    .filter(Boolean)
    .join(' ') || undefined

  return (
    <div className="w-full">
      {/* Label */}
      <label
        htmlFor={id}
        className="mb-1 block text-sm font-medium text-content-primary"
      >
        {label}
        {required && (
          <span aria-hidden="true" className="ms-0.5 text-error">
            {' '}*
          </span>
        )}
      </label>

      {/* Input */}
      <input
        id={id}
        required={required}
        aria-invalid={hasError || undefined}
        aria-describedby={describedBy}
        className={[
          'w-full rounded-md border bg-surface px-3 py-2 text-sm text-content-primary',
          'placeholder:text-content-muted',
          'transition-colors duration-150',
          'focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-1',
          'disabled:cursor-not-allowed disabled:opacity-60',
          hasError
            ? 'border-error focus-visible:ring-error'
            : 'border-stroke hover:border-stroke-strong focus-visible:ring-brand-500',
          className,
        ]
          .filter(Boolean)
          .join(' ')}
        {...props}
      />

      {/* Error */}
      {hasError && (
        <p id={errorId} role="alert" className="mt-1 text-xs text-error-dark">
          {error}
        </p>
      )}

      {/* Helper — only shows when no error */}
      {!hasError && helperText && (
        <p id={helperId} className="mt-1 text-xs text-content-muted">
          {helperText}
        </p>
      )}
    </div>
  )
}
