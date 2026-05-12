import { useNavigate }   from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageContainer }  from '@/components/layout/PageContainer'
import { StateWrapper }   from '@/components/feedback'
import { Button }         from '@/components/ui'
import { useMyChildren }  from '@/features/parent/hooks/useParent'

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatLastActivity(iso: string | null, neverLabel: string): string {
  if (!iso) return neverLabel
  const diffDays = Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000)
  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  return `${diffDays} days ago`
}

// ── Page ──────────────────────────────────────────────────────────────────────

/**
 * ChildrenPage — lists all students linked to the calling parent.
 *
 * Each row shows: name, grade, active-enrollment count, lessons-completed
 * count, and last-activity date.  A "View details" button navigates to
 * /parent/children/{studentId} for the full enrollment + progress breakdown.
 *
 * Empty state: rendered when the parent has no linked children.
 * Error state: rendered on network/API failure with a retry action.
 *
 * Phase 2: add a search/filter bar when a parent has many children.
 */
export default function ChildrenPage() {
  const { t }    = useTranslation()
  const navigate = useNavigate()

  const { data: children, isLoading, isError, error, refetch } = useMyChildren()

  return (
    <PageContainer
      title={t('parent.children.title')}
      description={t('parent.children.description')}
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!children?.length}
        emptyTitle={t('parent.children.empty')}
        emptyDescription={t('parent.children.emptyDescription')}
        onRetry={refetch}
      >
        <div className="overflow-hidden rounded-md border border-stroke bg-surface">
          <ul role="list" className="divide-y divide-stroke">
            {children?.map(child => (
              <li key={child.studentId} className="flex items-center gap-4 px-4 py-4">

                {/* Initials avatar */}
                <span
                  aria-hidden="true"
                  className="flex h-10 w-10 shrink-0 select-none items-center justify-center rounded-full bg-brand-100 text-sm font-bold uppercase text-brand-700"
                >
                  {child.fullName.charAt(0)}
                </span>

                {/* Info block */}
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold text-content-primary">
                    {child.fullName}
                  </p>
                  <p className="mt-0.5 text-xs text-content-secondary">
                    {child.gradeName ?? t('parent.children.noGrade')}
                    {' · '}
                    {child.activeEnrollments} {t('parent.children.enrollments')}
                    {' · '}
                    {child.lessonsCompleted} {t('parent.children.lessonsCompleted')}
                  </p>
                  <p className="mt-0.5 text-xs text-content-muted">
                    {t('parent.children.lastActive')}:{' '}
                    {formatLastActivity(child.lastActivityAt, t('parent.children.neverActive'))}
                  </p>
                </div>

                {/* Navigate to detail */}
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => navigate(`/parent/children/${child.studentId}`)}
                >
                  {t('parent.children.viewDetails')}
                </Button>
              </li>
            ))}
          </ul>
        </div>
      </StateWrapper>
    </PageContainer>
  )
}
