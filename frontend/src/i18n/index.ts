import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import LanguageDetector from 'i18next-browser-languagedetector'

import en from './locales/en.json'

/**
 * i18n initialization.
 *
 * - Default language: English (en)
 * - Language detection order: localStorage → browser navigator
 * - RTL/LTR direction is applied to the <html> element on language change
 *
 * To add a new language:
 * 1. Create src/i18n/locales/<lang>.json
 * 2. Import it here and add to resources
 * 3. Add to rtlLanguages if applicable
 */

const RTL_LANGUAGES = ['ar', 'fa', 'he', 'ur']

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      // Future: fa, ar, etc. will be added here
    },
    lng: 'en',
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false, // React already handles XSS escaping
    },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
    },
  })

// Apply document direction when language is initialized or changed
function applyDirection(lng: string) {
  const dir = RTL_LANGUAGES.includes(lng) ? 'rtl' : 'ltr'
  document.documentElement.setAttribute('lang', lng)
  document.documentElement.setAttribute('dir', dir)
}

i18n.on('initialized', () => applyDirection(i18n.language))
i18n.on('languageChanged', applyDirection)

export default i18n
