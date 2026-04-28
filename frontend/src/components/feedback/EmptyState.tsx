import type { ReactNode } from 'react'

// ── Props ─────────────────────────────────────────────────────────────────────

interface EmptyStateProps {
  /** Short heading — e.g. "No students yet" */
  title:        string
  /** Optional supporting description — e.g. "Enrol a student to get started." */
  description?: string
  /** Optional action element — typically a primary Button CTA. */
  action?:      ReactNode
  className?:   string
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * EmptyState — shown when a list, table, or data region has no items.
 *
 * Accessibility: `role="status"` so screen readers announce the state without
 * interrupting the user (live region that waits before announcing).
 *
 * Usage:
 *   <EmptyState
 *     title="No courses yet"
 *     description="Create your first course to get started."
 *     action={<Button onClick={create}>Create course</Button>}
 *   />
 *
 * Phase 2 TODO: add an optional `icon` prop (ReactNode) once lucide-react
 * is added as a dependency. The SVG below is an inline placeholder.
 */
export function EmptyState({ title, description, action, className = '' }: EmptyStateProps) {
  return (
    <div
      role="status"
      className={[
        'flex flex-col items-center justify-center px-6 py-12 text-center',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {/* Placeholder illustration — replace with lucide-react icon in Phase 2 */}
      <svg
        aria-hidden="true"
        className="mb-4 h-12 w-12 text-content-muted"
        fill="none"
        viewBox="0 0 48 48"
        stroke="currentColor"
        strokeWidth={1.25}
      >
        {/* Open box */}
        <rect x="8" y="18" width="32" height="22" rx="2" />
        <path strokeLinecap="round" d="M8 24h32" />
        <path strokeLinecap="round" d="M17 8 11 18M31 8l6 10" />
        <path strokeLinecap="round" d="M19 33h10" />
      </svg>

      <h3 className="text-sm font-semibold text-content-primary">{title}</h3>

      {description && (
        <p className="mt-1 max-w-xs text-sm text-content-secondary">{description}</p>
      )}

      {action && <div className="mt-4">{action}</div>}
    </div>
  )
}
