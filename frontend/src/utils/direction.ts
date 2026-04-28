import type { Direction, Locale } from '@/types'

/**
 * Canonical set of RTL locale codes.
 * Import this — do not define your own copy.
 * Add new RTL locales here and all consumers pick up the change automatically.
 */
export const RTL_LOCALES = new Set<Locale>(['ar', 'fa', 'he', 'ur'])

/**
 * Returns the text direction for a given locale.
 */
export function getDirection(locale: Locale): Direction {
  return RTL_LOCALES.has(locale) ? 'rtl' : 'ltr'
}

/**
 * Returns true if a locale uses right-to-left layout.
 */
export function isRtl(locale: Locale): boolean {
  return RTL_LOCALES.has(locale)
}

/**
 * Applies lang and dir attributes to the <html> element.
 * Called by the i18n module on language change.
 */
export function applyDocumentDirection(locale: Locale): void {
  const dir = getDirection(locale)
  document.documentElement.setAttribute('lang', locale)
  document.documentElement.setAttribute('dir', dir)
}
