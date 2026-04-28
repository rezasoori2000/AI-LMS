import { useTranslation } from 'react-i18next'
import type { Direction } from '@/types'
import { isRtl } from '@/utils/direction'

/**
 * Returns the current document direction ('ltr' | 'rtl').
 * Re-renders automatically when the i18n language changes.
 *
 * Prefer the `rtl:` Tailwind variant for pure CSS direction changes.
 * Use this hook only when direction must be read in JavaScript
 * (e.g., calculating popover offset, flipping a scroll animation).
 */
export function useDirection(): Direction {
  const { i18n } = useTranslation()
  return isRtl(i18n.language) ? 'rtl' : 'ltr'
}
