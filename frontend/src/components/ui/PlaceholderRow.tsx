// ── Types ─────────────────────────────────────────────────────────────────────

type RowAccent = 'brand' | 'success' | 'warning' | 'error' | 'info' | 'muted'

const ACCENT_DOT: Record<RowAccent, string> = {
  brand:   'bg-brand-500',
  success: 'bg-success',
  warning: 'bg-warning',
  error:   'bg-error',
  info:    'bg-info',
  muted:   'bg-content-muted',
}

// ── Props ─────────────────────────────────────────────────────────────────────

interface PlaceholderRowProps {
  /** Primary label — entity name or action description. */
  label:   string
  /** Supporting context below the label — e.g. "24 students", "Due tomorrow". */
  meta?:   string
  /** Short classification tag shown at the trailing edge. */
  tag?:    string
  /** Dot colour communicating rough status or category. Defaults to 'muted'. */
  accent?: RowAccent
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * PlaceholderRow — a static list-item row used inside dashboard SectionCards
 * to hint at the future data shape without requiring a backend connection.
 *
 * Layout (inline, full-width):
 *   [accent dot]  [label / meta]             …             [tag]
 *
 * Usage inside a SectionCard:
 *   <SectionCard title="My Classes">
 *     <PlaceholderRow label="Grade 7A — Mathematics" meta="24 students" accent="brand" />
 *     <PlaceholderRow label="Grade 8 — English"      meta="18 students" accent="brand" />
 *   </SectionCard>
 *
 * Phase 2 migration:
 *   Swap PlaceholderRow items for real data-driven list components.
 *   The outer SectionCard structure (title, description, action) stays unchanged.
 *
 * RTL: no directional Tailwind classes needed — the flex layout follows the
 * document writing direction automatically. `min-w-0 flex-1` on the label
 * block ensures text truncation works correctly in both directions.
 */
export function PlaceholderRow({ label, meta, tag, accent = 'muted' }: PlaceholderRowProps) {
  return (
    <div className="flex items-center gap-3 py-2.5 border-b border-stroke last:border-b-0">
      {/* Accent dot */}
      <span
        aria-hidden="true"
        className={['h-2 w-2 flex-shrink-0 rounded-full', ACCENT_DOT[accent]].join(' ')}
      />

      {/* Label + meta */}
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-content-primary truncate">{label}</p>
        {meta && (
          <p className="text-xs text-content-muted truncate">{meta}</p>
        )}
      </div>

      {/* Trailing tag */}
      {tag && (
        <span className="flex-shrink-0 rounded-full bg-surface-overlay px-2 py-0.5 text-[11px] font-medium text-content-secondary">
          {tag}
        </span>
      )}
    </div>
  )
}
