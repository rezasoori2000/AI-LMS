import type { Direction, Locale } from '@/types'

const RTL_LANGUAGES: Locale[] = ['ar', 'fa', 'he', 'ur']

/**
 * Returns the text direction for a given locale.
 */
export function getDirection(locale: Locale): Direction {
  return RTL_LANGUAGES.includes(locale) ? 'rtl' : 'ltr'
}

/**
 * Returns true if a locale uses right-to-left layout.
 */
export function isRtl(locale: Locale): boolean {
  return getDirection(locale) === 'rtl'
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
