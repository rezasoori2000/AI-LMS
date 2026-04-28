import type { ReactNode } from 'react'

// ── Types ─────────────────────────────────────────────────────────────────────

export type FeedbackIntent = 'success' | 'warning' | 'error' | 'info'

// ── Props ─────────────────────────────────────────────────────────────────────

interface InlineFeedbackProps {
  /** Visual and semantic intent of the message. */
  intent: FeedbackIntent
  /** The feedback text or node to display. */
  message: ReactNode
  className?: string
}

// ── Style map ─────────────────────────────────────────────────────────────────

const INTENT_CLASSES: Record<FeedbackIntent, string> = {
  success: 'bg-success-light border-success text-success-dark',
  warning: 'bg-warning-light border-warning text-warning-dark',
  error:   'bg-error-light   border-error   text-error-dark',
  info:    'bg-info-light    border-info    text-info-dark',
}

// error and warning are immediate ("alert"); success and info are polite ("status")
const ALERT_INTENTS = new Set<FeedbackIntent>(['error', 'warning'])

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * InlineFeedback — a compact banner for inline status messages.
 *
 * Use for:
 * - Form-level submit feedback ("Changes saved." / "Failed to save.")
 * - Section-level notices ("This feature is read-only in demo mode.")
 * - TanStack Query mutation status (wire isSuccess/isError message here)
 *
 * Not a toast — this renders in document flow. For transient ephemeral
 * notifications that appear over content, add a toast provider in Phase 2.
 *
 * Accessibility:
 * - `role="alert"` for error/warning: screen readers announce immediately.
 * - `role="status"` for success/info: polite announcement, less disruptive.
 *
 * RTL: uses only non-directional classes (rounded, border, px/py). Safe.
 *
 * Usage:
 *   {saveError && <InlineFeedback intent="error" message="Failed to save." />}
 *   {saveSuccess && <InlineFeedback intent="success" message="Changes saved." />}
 *   <InlineFeedback intent="info" message="Read-only in demo mode." />
 */
export function InlineFeedback({
  intent,
  message,
  className = '',
}: InlineFeedbackProps) {
  return (
    <div
      role={ALERT_INTENTS.has(intent) ? 'alert' : 'status'}
      className={[
        'rounded-md border px-4 py-3 text-sm',
        INTENT_CLASSES[intent],
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {message}
    </div>
  )
}
