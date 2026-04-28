import type { HTMLAttributes, ReactNode } from 'react'

// ── Card root ─────────────────────────────────────────────────────────────────

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
}

/**
 * Surface card — the primary container for dashboard content blocks.
 *
 * Tenant theming: background uses `bg-surface`, border uses `border-stroke`.
 * Both resolve to CSS variables so tenants can override the card appearance
 * without changing component code.
 *
 * Compose with CardHeader and CardBody:
 *   <Card>
 *     <CardHeader title="Students" description="This week" />
 *     <CardBody>…</CardBody>
 *   </Card>
 */
export function Card({ children, className = '', ...props }: CardProps) {
  return (
    <div
      className={['bg-surface border border-stroke rounded-lg shadow-card overflow-hidden', className]
        .filter(Boolean)
        .join(' ')}
      {...props}
    >
      {children}
    </div>
  )
}

// ── CardHeader ────────────────────────────────────────────────────────────────

interface CardHeaderProps {
  title:        string
  description?: string
  /** Optional action element rendered at the trailing edge (RTL-aware via ms-4). */
  action?:      ReactNode
}

export function CardHeader({ title, description, action }: CardHeaderProps) {
  return (
    <div className="flex items-start justify-between px-5 py-4 border-b border-stroke">
      <div>
        <h3 className="text-sm font-semibold text-content-primary">{title}</h3>
        {description && (
          <p className="mt-0.5 text-xs text-content-secondary">{description}</p>
        )}
      </div>
      {action && <div className="ms-4 flex-shrink-0">{action}</div>}
    </div>
  )
}

// ── CardBody ──────────────────────────────────────────────────────────────────

interface CardBodyProps {
  children:   ReactNode
  className?: string
}

export function CardBody({ children, className = '' }: CardBodyProps) {
  return (
    <div className={['px-5 py-4', className].filter(Boolean).join(' ')}>
      {children}
    </div>
  )
}
