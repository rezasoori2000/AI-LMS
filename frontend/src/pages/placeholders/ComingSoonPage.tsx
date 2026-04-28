import { Link, useMatches, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageContainer } from '@/components/layout/PageContainer'
import { Badge } from '@/components/ui/Badge'
import type { RouteHandle } from '@/routes/types'

/**
 * ComingSoonPage — a single shared placeholder for all sub-routes that are
 * defined in the route tree but not yet implemented.
 *
 * Why a shared page instead of individual placeholder files:
 * - Avoids 15+ nearly identical files in pages/
 * - Reads the page title from the route's handle.titleKey automatically
 * - Provides a working "back" link derived from the current URL
 * - When a real page is built, just swap `element: <ComingSoonPage />` for
 *   the real page component — the route path and handle stay the same.
 *
 * Title resolution:
 *   useMatches().at(-1)?.handle  gives the deepest matched route's handle.
 *   If the route has handle.titleKey, t(titleKey) is used as the page title.
 *   Falls back to a generic "Coming soon" heading if handle is not set.
 */
export default function ComingSoonPage() {
  const { t } = useTranslation()
  const matches = useMatches()
  const { pathname } = useLocation()

  const handle = matches.slice(-1)[0]?.handle as RouteHandle | undefined
  const title = handle?.titleKey ? t(handle.titleKey) : t('common.comingSoon')

  // Derive the parent dashboard path:
  //   /admin/tenants  → /admin
  //   /teacher/lessons → /teacher
  // Falls back to '/' for anything unexpected.
  const parentPath = pathname.split('/').slice(0, -1).join('/') || '/'

  return (
    <PageContainer title={title} description={t('common.comingSoon')}>
      <div className="max-w-md space-y-4">
        <Badge variant="info">Coming in a future phase</Badge>

        <p className="text-sm text-content-secondary">
          This section is under active development. It will be available in a
          future phase of the project.
        </p>

        <Link
          to={parentPath}
          className={[
            'inline-flex items-center gap-1.5 rounded-md border border-stroke',
            'bg-surface px-4 py-2 text-sm font-medium text-content-primary',
            'hover:bg-surface-raised transition-colors duration-150',
            'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500',
          ].join(' ')}
        >
          {/* Logical arrow: points left in LTR, right in RTL */}
          <span aria-hidden="true">←</span>
          {t('common.back')}
        </Link>
      </div>
    </PageContainer>
  )
}
