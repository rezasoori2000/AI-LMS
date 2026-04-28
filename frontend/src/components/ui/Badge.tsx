import type { ReactNode } from 'react'

type BadgeVariant = 'default' | 'success' | 'warning' | 'error' | 'info'

const VARIANT_CLASSES: Record<BadgeVariant, string> = {
  default: 'bg-surface-overlay text-content-secondary',
  success: 'bg-success-light text-success-dark',
  warning: 'bg-warning-light text-warning-dark',
  error:   'bg-error-light   text-error-dark',
  info:    'bg-info-light    text-info-dark',
}

interface BadgeProps {
  children:   ReactNode
  variant?:   BadgeVariant
  className?: string
}

/**
 * Status badge for labels, tags, and state indicators.
 *
 * Usage:
 *   <Badge variant="success">Active</Badge>
 *   <Badge variant="warning">Pending review</Badge>
 */
export function Badge({ children, variant = 'default', className = '' }: BadgeProps) {
  return (
    <span
      className={[
        'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
        VARIANT_CLASSES[variant],
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {children}
    </span>
  )
}
