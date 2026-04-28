import { Card, CardBody } from './Card'

// ── Props ─────────────────────────────────────────────────────────────────────

interface StatCardProps {
  /** Short label shown above the value — e.g. "Total Tenants" */
  label:      string
  /** Primary numeric or text value — e.g. "1,240" or "—" */
  value:      string | number
  /** Optional supporting note below the value — e.g. "vs last month" */
  subLabel?:  string
  className?: string
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * StatCard — a compact metric display card for dashboard grids.
 *
 * Use in a responsive grid:
 *   <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
 *     <StatCard label="Students" value="1,240" subLabel="+12 this week" />
 *     …
 *   </div>
 *
 * Phase 2: extend with a `trend` prop ({ direction: 'up'|'down', percent: number })
 * and render a coloured indicator arrow. The current interface is forward-compatible:
 * just add the optional prop — no existing call sites change.
 */
export function StatCard({ label, value, subLabel, className }: StatCardProps) {
  return (
    <Card className={className}>
      <CardBody>
        <p className="text-xs font-semibold uppercase tracking-widest text-content-muted">
          {label}
        </p>
        <p className="mt-2 text-2xl font-bold text-content-primary">{value}</p>
        {subLabel && (
          <p className="mt-0.5 text-xs text-content-secondary">{subLabel}</p>
        )}
      </CardBody>
    </Card>
  )
}
