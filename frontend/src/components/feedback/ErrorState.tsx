import { Button } from '@/components/ui/Button'

// ── Props ─────────────────────────────────────────────────────────────────────

interface ErrorStateProps {
  /** Short heading. Defaults to "Something went wrong". */
  title?:     string
  /** Required descriptive message — pass `error.message` or a user-friendly string. */
  message:    string
  /** When provided, renders a "Try again" button that calls this handler. */
  onRetry?:   () => void
  className?: string
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * ErrorState — shown when a data fetch or operation fails.
 *
 * Accessibility: `role="alert"` causes screen readers to announce this
 * immediately when it appears, which is appropriate for error conditions.
 *
 * Usage:
 *   {isError && (
 *     <ErrorState
 *       message={error.message}
 *       onRetry={refetch}
 *     />
 *   )}
 *
 * With a custom title:
 *   <ErrorState
 *     title="Could not load students"
 *     message="Please check your connection and try again."
 *     onRetry={refetch}
 *   />
 */
export function ErrorState({
  title     = 'Something went wrong',
  message,
  onRetry,
  className = '',
}: ErrorStateProps) {
  return (
    <div
      role="alert"
      className={[
        'flex flex-col items-center justify-center px-6 py-12 text-center',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {/* Warning triangle — uses error token colour */}
      <svg
        aria-hidden="true"
        className="mb-4 h-12 w-12 text-error"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
        strokeWidth={1.5}
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z"
        />
      </svg>

      <h3 className="text-sm font-semibold text-content-primary">{title}</h3>

      <p className="mt-1 max-w-xs text-sm text-content-secondary">{message}</p>

      {onRetry && (
        <div className="mt-4">
          <Button variant="secondary" size="sm" onClick={onRetry}>
            Try again
          </Button>
        </div>
      )}
    </div>
  )
}
