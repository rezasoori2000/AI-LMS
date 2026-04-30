import { useTranslation } from 'react-i18next'

// ── Types ─────────────────────────────────────────────────────────────────────

type LoadingSize = 'sm' | 'md' | 'lg'

const SPINNER_SIZE: Record<LoadingSize, string> = {
  sm: 'h-5 w-5',
  md: 'h-8 w-8',
  lg: 'h-12 w-12',
}

// ── Props ─────────────────────────────────────────────────────────────────────

interface LoadingStateProps {
  /** Screen-reader label and optional visible caption. Defaults to "Loading…". */
  message?:   string
  size?:      LoadingSize
  className?: string
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * LoadingState — a centred spinner shown while data is being fetched.
 *
 * Accessibility:
 * - `role="status"` with `aria-label` announces the loading message to
 *   screen readers without being overly disruptive (polite live region).
 * - The SVG is `aria-hidden`; the accessible name comes from `aria-label`.
 *
 * Usage:
 *   {isLoading && <LoadingState message="Loading students…" />}
 *   {isLoading && <LoadingState size="sm" />}
 *
 * Inline variant — wrap in a sized container to limit the vertical space:
 *   <div className="py-8">
 *     <LoadingState size="sm" />
 *   </div>
 */
export function LoadingState({
  message,
  size      = 'md',
  className = '',
}: LoadingStateProps) {
  const { t } = useTranslation()
  const label = message ?? t('common.loading')

  return (
    <div
      role="status"
      aria-label={label}
      className={[
        'flex flex-col items-center justify-center gap-3 px-6 py-12',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {/* Spinning SVG — colour follows brand token, works in dark mode */}
      <svg
        aria-hidden="true"
        className={['animate-spin text-brand-500', SPINNER_SIZE[size]].join(' ')}
        xmlns="http://www.w3.org/2000/svg"
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

      {/* Visible caption (optional; always used as aria-label regardless) */}
      {message && (
        <p aria-hidden="true" className="text-sm text-content-secondary">
          {message}
        </p>
      )}
    </div>
  )
}
