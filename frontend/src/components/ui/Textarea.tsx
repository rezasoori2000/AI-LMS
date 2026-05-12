import { useId, type TextareaHTMLAttributes } from 'react'

// ── Props ─────────────────────────────────────────────────────────────────────

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label:       string
  helperText?: string
  error?:      string
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * Accessible textarea primitive — same accessibility and styling contract
 * as the Input component.
 *
 * Phase 3: swap for a rich Markdown editor (e.g. CodeMirror) when rich lesson
 * authoring is in scope. The label/error/helperText props will remain unchanged.
 */
export function Textarea({
  label,
  helperText,
  error,
  id:        idProp,
  required,
  className = '',
  rows = 6,
  ...props
}: TextareaProps) {
  const generatedId = useId()
  const id          = idProp ?? generatedId
  const helperId    = `${id}-helper`
  const errorId     = `${id}-error`
  const hasError    = Boolean(error)

  const describedBy = [hasError && errorId, !hasError && helperText && helperId]
    .filter(Boolean)
    .join(' ') || undefined

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="text-sm font-medium text-content-primary">
        {label}
        {required && (
          <span aria-hidden="true" className="ms-1 text-error">
            *
          </span>
        )}
      </label>

      <textarea
        id={id}
        rows={rows}
        required={required}
        aria-invalid={hasError || undefined}
        aria-describedby={describedBy}
        className={[
          'block w-full rounded-md border px-3 py-2 text-sm',
          'bg-surface text-content-primary placeholder:text-content-muted',
          'transition-colors duration-150 resize-y',
          'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-1',
          hasError
            ? 'border-error focus-visible:ring-error'
            : 'border-stroke hover:border-brand-400',
          'disabled:opacity-50 disabled:cursor-not-allowed',
          className,
        ]
          .filter(Boolean)
          .join(' ')}
        {...props}
      />

      {!hasError && helperText && (
        <p id={helperId} className="text-xs text-content-secondary">
          {helperText}
        </p>
      )}

      {hasError && (
        <p id={errorId} role="alert" className="text-xs text-error">
          {error}
        </p>
      )}
    </div>
  )
}
