import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageContainer } from '@/components/layout/PageContainer'
import { Card, CardBody } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'

const DASHBOARDS = [
  { href: '/admin',   label: 'Admin Dashboard',   description: 'Platform and tenant management', badge: 'super_admin / tenant_admin' },
  { href: '/teacher', label: 'Teacher Dashboard',  description: 'Classes, lessons, student progress', badge: 'teacher' },
  { href: '/parent',  label: 'Parent Dashboard',   description: 'Children\'s learning overview', badge: 'parent' },
  { href: '/student', label: 'Student Dashboard',  description: 'Lessons, AI tutor, progress', badge: 'student' },
] as const

/**
 * HomePage — development navigation hub.
 *
 * In production (Phase 2) this page is replaced by a post-login redirect to the
 * current user's dashboard based on their role. It is kept only so developers
 * can navigate without auth during the UI-shell phase.
 */
export default function HomePage() {
  const { t } = useTranslation()

  return (
    <PageContainer
      title={t('home.title')}
      description={t('home.subtitle')}
    >
      {/* Status badge */}
      <div className="mb-6 inline-flex items-center gap-2 text-sm text-green-700 bg-green-50 border border-green-200 rounded-lg px-4 py-2">
        <span aria-hidden="true" className="w-2 h-2 rounded-full bg-green-500 inline-block" />
        Phase 1 Section 2 — UI shell complete
      </div>

      {/* Dashboard navigation cards */}
      <p className="mb-4 text-sm font-medium text-content-secondary">
        Navigate to a dashboard (no auth required during development):
      </p>
      <div className="grid gap-4 sm:grid-cols-2">
        {DASHBOARDS.map(({ href, label, description, badge }) => (
          <Link key={href} to={href} className="group block focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 rounded-lg">
            <Card className="h-full transition-shadow duration-150 group-hover:shadow-md">
              <CardBody>
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-semibold text-content-primary group-hover:text-brand-600 transition-colors">
                      {label}
                    </p>
                    <p className="mt-1 text-sm text-content-secondary">{description}</p>
                  </div>
                  <Badge variant="default" className="flex-shrink-0">{badge}</Badge>
                </div>
              </CardBody>
            </Card>
          </Link>
        ))}
      </div>

      <p className="mt-6 text-xs text-content-muted">
        Phase 2 will add login, role-based route guards, and automatic dashboard redirect.
      </p>
    </PageContainer>
  )
}
