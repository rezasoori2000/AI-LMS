import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

/**
 * NotFoundPage — shown for any unmatched route.
 */
export default function NotFoundPage() {
  const { t } = useTranslation()

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="text-center">
        <p className="text-6xl font-bold text-gray-200">404</p>
        <h1 className="mt-4 text-2xl font-semibold text-gray-800">
          {t('errors.notFound')}
        </h1>
        <p className="mt-2 text-gray-500">
          The page you are looking for does not exist or has been moved.
        </p>
        <Link
          to="/"
          className="mt-6 inline-block px-5 py-2.5 bg-brand-600 text-white text-sm font-medium rounded-lg hover:bg-brand-700 transition-colors"
        >
          {t('errors.goHome')}
        </Link>
      </div>
    </div>
  )
}
