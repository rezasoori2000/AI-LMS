import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation }         from 'react-i18next'
import { PageContainer }           from '@/components/layout/PageContainer'
import { StateWrapper }            from '@/components/feedback'
import { Badge, Button, SectionCard, StatCard } from '@/components/ui'
import { useChildDetail }          from '@/features/parent/hooks/useParent'
import { formatDate, formatLastActivity } from '@/utils/dateFormat'
import type { EnrollmentSummaryDto, EnrollmentStatus } from '@/types/parent'

// ── Helpers ───────────────────────────────────────────────────────────────────

const STATUS_VARIANT: Record<EnrollmentStatus, 'success' | 'info' | 'default'> = {
  Active:    'info',
  Completed: 'success',
  Dropped:   'default',
}

// ── Sub-components ────────────────────────────────────────────────────────────

interface EnrollmentRowProps {
  enrollment: EnrollmentSummaryDto
}

function EnrollmentRow({ enrollment: e }: EnrollmentRowProps) {
  const { t } = useTranslation()

  return (
    <div className="border-b border-stroke px-5 py-4 last:border-0">
      {/* Header row: subject name + status badge + enrolled date */}
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-sm font-semibold text-content-primary">
          {e.subjectName}
        </span>
        <Badge variant={STATUS_VARIANT[e.status]}>
          {t(`parent.enrollmentStatus.${e.status}`)}
        </Badge>
        <span className="ms-auto text-xs text-content-muted">
          {t('parent.childDetail.enrolledOn')}: {formatDate(e.enrolledAt)}
        </span>
      </div>

      {/* Progress row: lesson counts + average score */}
      <div className="mt-1.5 flex flex-wrap gap-x-5 gap-y-1 text-xs text-content-secondary">
        <span>
          <span className="font-medium text-content-primary">{e.totalLessons}</span>
          {' '}{t('parent.childDetail.totalLessons')}
        </span>
        <span>
          <span className="font-medium text-content-primary">{e.completedLessons}</span>
          {' '}{t('parent.childDetail.completed')}
        </span>
        <span>
          <span className="font-medium text-content-primary">{e.inProgressLessons}</span>
          {' '}{t('parent.childDetail.inProgress')}
        </span>
        <span>
          {t('parent.childDetail.averageScore')}:{' '}
          <span className="font-medium text-content-primary">
            {e.averageScore !== null
              ? `${Math.round(e.averageScore)}%`
              : t('parent.childDetail.notScored')}
          </span>
        </span>
      </div>
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

/**
 * ChildDetailPage — full breakdown for one linked student.
 *
 * Shows:
 *   - Quick-stat row: grade, active enrollments, lessons completed, last activity
 *   - Enrollments section: per-subject progress summary (lesson counts + avg score)
 *
 * Access control is enforced server-side — a 403 response renders the
 * ErrorState rather than a blank page.
 *
 * Phase 2: add a per-lesson progress timeline below the enrollments section.
 * Phase 2: add a performance trend chart (week-by-week average score).
 */
export default function ChildDetailPage() {
  const { t }         = useTranslation()
  const navigate      = useNavigate()
  const { studentId } = useParams<{ studentId: string }>()

  const { data, isLoading, isError, error, refetch } = useChildDetail(studentId)

  const summary = data?.summary

  return (
    <PageContainer
      title={summary?.fullName ?? t('parent.childDetail.title')}
      description={summary?.gradeName ?? undefined}
      actions={
        <Button
          variant="secondary"
          size="sm"
          onClick={() => navigate('/parent/children')}
        >
          ← {t('parent.childDetail.back')}
        </Button>
      }
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={false}
        onRetry={refetch}
      >
        <div className="space-y-6">

          {/* ── Quick stats ──────────────────────────────────────────────── */}
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              label={t('parent.childDetail.grade')}
              value={summary?.gradeName ?? '—'}
            />
            <StatCard
              label={t('parent.childDetail.activeEnrollments')}
              value={summary?.activeEnrollments ?? '—'}
            />
            <StatCard
              label={t('dashboards.parent.stats.lessonsCompleted')}
              value={summary?.lessonsCompleted ?? '—'}
            />
            <StatCard
              label={t('parent.children.lastActive')}
              value={formatLastActivity(summary?.lastActivityAt ?? null)}
            />
          </div>

          {/* ── Enrollments ──────────────────────────────────────────────── */}
          <SectionCard
            title={t('parent.childDetail.enrollments')}
            bodyClassName="!p-0"
          >
            {!data?.enrollments.length ? (
              <p className="px-5 py-6 text-sm text-content-secondary">
                {t('parent.childDetail.noEnrollments')}
              </p>
            ) : (
              data.enrollments.map(e => (
                <EnrollmentRow key={e.enrollmentId} enrollment={e} />
              ))
            )}
          </SectionCard>

        </div>
      </StateWrapper>
    </PageContainer>
  )
}
