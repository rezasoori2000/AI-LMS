import type { ReactNode } from 'react'

interface PageContainerProps {
  /** Page title — rendered as an <h1>. Required for accessibility. */
  title:        string
  /** Optional supporting description below the title. */
  description?: string
  /**
   * Optional action elements rendered at the trailing edge of the header row.
   * Use for page-level CTA buttons (e.g., "New course", "Export").
   * RTL-safe: uses `ms-auto` so it correctly moves to the left edge in RTL.
   */
  actions?:     ReactNode
  children:     ReactNode
  className?:   string
}

/**
 * PageContainer — the standard content wrapper for all dashboard pages.
 *
 * Provides:
 * - Consistent horizontal padding (px-4 mobile → px-6 desktop)
 * - Consistent vertical padding (py-6 mobile → py-8 desktop)
 * - A semantic page header with <h1>, optional description, and action slot
 * - A `<section>` wrapper with `aria-labelledby` for screen reader page structure
 *
 * Usage:
 *   <PageContainer title="Students" description="All enrolled students" actions={<Button>Enroll</Button>}>
 *     <DataTable … />
 *   </PageContainer>
 */
export function PageContainer({
  title,
  description,
  actions,
  children,
  className = '',
}: PageContainerProps) {
  const headingId = `page-heading-${title.toLowerCase().replace(/\s+/g, '-')}`

  return (
    <section
      aria-labelledby={headingId}
      className={['px-4 py-6 md:px-6 md:py-8 max-w-7xl mx-auto w-full', className]
        .filter(Boolean)
        .join(' ')}
    >
      {/* Page header */}
      <div className="mb-6 flex flex-wrap items-start gap-4">
        <div className="flex-1 min-w-0">
          <h1
            id={headingId}
            className="text-2xl font-bold text-content-primary truncate"
          >
            {title}
          </h1>
          {description && (
            <p className="mt-1 text-sm text-content-secondary">{description}</p>
          )}
        </div>
        {actions && (
          <div className="flex flex-shrink-0 items-center gap-2 ms-auto">
            {actions}
          </div>
        )}
      </div>

      {/* Page body */}
      {children}
    </section>
  )
}
