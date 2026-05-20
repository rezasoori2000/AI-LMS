/**
 * Shared date-formatting helpers used across portal pages.
 *
 * NOTE: `formatLastActivity` returns English strings for now; i18n support is
 * deferred until the translation pipeline covers utility functions (Phase 2).
 */

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year:  'numeric',
    month: 'short',
    day:   'numeric',
  })
}

export function formatLastActivity(iso: string | null): string {
  if (!iso) return '—'
  const diffDays = Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000)
  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  return `${diffDays}d ago`
}
