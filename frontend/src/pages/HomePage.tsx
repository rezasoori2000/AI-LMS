import { useTranslation } from 'react-i18next'

/**
 * HomePage — placeholder landing page.
 * Will be replaced with role-based dashboards in Section 2+.
 */
export default function HomePage() {
  const { t } = useTranslation()

  return (
    <div className="text-center py-16">
      <h1 className="text-3xl font-bold text-gray-900">
        {t('home.title')}
      </h1>
      <p className="mt-3 text-lg text-gray-600">
        {t('home.subtitle')}
      </p>
      <div className="mt-8 inline-flex items-center gap-2 text-sm text-green-600 bg-green-50 border border-green-200 rounded-lg px-4 py-2">
        <span className="w-2 h-2 rounded-full bg-green-500 inline-block" />
        Foundation scaffold ready — Phase 1, Section 1
      </div>
    </div>
  )
}
