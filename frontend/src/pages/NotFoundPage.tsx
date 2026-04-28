import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

/**
 * NotFoundPage — shown for any unmatched route.
 */
export default function NotFoundPage() {
  const { t } = useTranslation()

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface-raised px-4">
      <div className="text-center">
        <p className="text-6xl font-bold text-content-muted">404</p>
        <h1 className="mt-4 text-2xl font-semibold text-content-primary">
          {t('errors.notFound')}
        </h1>
        <p className="mt-2 text-sm text-content-secondary">
          The page you are looking for does not exist or has been moved.
        </p>
        <Link
          to="/"
          className={[
            'mt-6 inline-block px-5 py-2.5 rounded-md',
            'bg-brand-600 text-white text-sm font-medium',
            'hover:bg-brand-700 transition-colors duration-150',
            'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2',
          ].join(' ')}
        >
          {t('errors.goHome')}
        </Link>
      </div>
    </div>
  )
}
