// ── SkeletonBlock ─────────────────────────────────────────────────────────────

interface SkeletonBlockProps {
  /** Tailwind height class, e.g. "h-4", "h-10", "h-32". Defaults to "h-4". */
  height?: string
  /** Tailwind width class, e.g. "w-24", "w-full". Defaults to "w-full". */
  width?: string
  className?: string
}

/**
 * SkeletonBlock — a single pulsing placeholder rectangle.
 *
 * Accessibility: `aria-hidden="true"` — purely decorative. The containing
 * region should provide an accessible loading label via an adjacent LoadingState
 * or an `aria-busy="true"` wrapper (Phase 2 enhancement).
 *
 * Dark mode: `bg-stroke` uses the CSS token that has a dark-mode override in
 * index.css, so no `dark:` variant is needed here.
 *
 * Usage:
 *   <SkeletonBlock height="h-32" />               // chart / image placeholder
 *   <SkeletonBlock height="h-4" width="w-3/4" />  // heading line
 *
 *   // Section-loading pattern (section stays mounted, body shows skeleton):
 *   <SectionCard title="Recent Activity">
 *     {isLoading ? <SkeletonText /> : <ActivityList />}
 *   </SectionCard>
 */
export function SkeletonBlock({
  height    = 'h-4',
  width     = 'w-full',
  className = '',
}: SkeletonBlockProps) {
  return (
    <div
      aria-hidden="true"
      className={['animate-pulse rounded-md bg-stroke', height, width, className]
        .filter(Boolean)
        .join(' ')}
    />
  )
}

// ── SkeletonText ──────────────────────────────────────────────────────────────

interface SkeletonTextProps {
  /** Number of lines to render. Defaults to 3. */
  lines?: number
  /** Height of each line. Defaults to "h-4". */
  lineHeight?: string
  className?: string
}

/**
 * SkeletonText — stacked SkeletonBlock rows that mimic a text passage.
 *
 * The final line is rendered at 2/3 width to suggest a natural paragraph end.
 *
 * Usage:
 *   <SkeletonText />             // 3-line paragraph placeholder
 *   <SkeletonText lines={5} />   // 5-line list placeholder
 */
export function SkeletonText({
  lines      = 3,
  lineHeight = 'h-4',
  className  = '',
}: SkeletonTextProps) {
  return (
    <div
      aria-hidden="true"
      className={['space-y-2', className].filter(Boolean).join(' ')}
    >
      {Array.from({ length: lines }, (_, i) => (
        <SkeletonBlock
          key={i}
          height={lineHeight}
          width={i === lines - 1 ? 'w-2/3' : 'w-full'}
        />
      ))}
    </div>
  )
}
