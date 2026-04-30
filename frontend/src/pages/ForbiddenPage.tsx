import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

/**
 * ForbiddenPage (403) — shown when a role guard denies access.
 *
 * Phase 2 usage:
 *   The auth loader in routes/index.tsx checks `handle.access` and throws:
 *     throw redirect('/403')
 *   when the authenticated user's role is not listed in the route's access array.
 *
 * Rendered OUTSIDE AppLayout intentionally — an unauthorised user should not
 * see the sidebar navigation for a section they cannot access.
 */
export default function ForbiddenPage() {
  const { t } = useTranslation()

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface-raised px-4">
      <div className="text-center">
        <p className="text-6xl font-bold text-content-muted">403</p>
        <h1 className="mt-4 text-2xl font-semibold text-content-primary">
          {t('errors.forbidden')}
        </h1>
        <p className="mt-2 text-sm text-content-secondary">
          {t('errors.unauthorized')}
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
