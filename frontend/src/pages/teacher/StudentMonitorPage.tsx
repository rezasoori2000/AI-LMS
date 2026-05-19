import { useState }                    from 'react'
import { useNavigate, useParams }      from 'react-router-dom'
import { useTranslation }              from 'react-i18next'
import { PageContainer }               from '@/components/layout/PageContainer'
import { StateWrapper }                from '@/components/feedback'
import { Badge, Button, SectionCard, StatCard } from '@/components/ui'
import { useStudentDetail, useStudentProgress } from '@/features/teacher/hooks/useTeacher'
import type {
  TeacherEnrollmentItemDto,
  TeacherLessonProgressItemDto,
  EnrollmentStatus,
  ProgressStatus,
} from '@/types/teacher'

// ── Helpers ───────────────────────────────────────────────────────────────────

const ENROLLMENT_VARIANT: Record<EnrollmentStatus, 'success' | 'info' | 'default'> = {
  Active:    'info',
  Completed: 'success',
  Dropped:   'default',
}

const PROGRESS_VARIANT: Record<ProgressStatus, 'success' | 'warning' | 'default'> = {
  NotStarted: 'default',
  InProgress: 'warning',
  Completed:  'success',
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year:  'numeric',
    month: 'short',
    day:   'numeric',
  })
}

function formatLastActivity(iso: string | null): string {
  if (!iso) return '—'
  const diffDays = Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000)
  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  return `${diffDays}d ago`
}

// ── Sub-components ────────────────────────────────────────────────────────────

function EnrollmentRow({ enrollment: e }: { enrollment: TeacherEnrollmentItemDto }) {
  const { t } = useTranslation()
  return (
    <div className="border-b border-stroke px-5 py-4 last:border-0">
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-sm font-semibold text-content-primary">{e.subjectName}</span>
        <Badge variant={ENROLLMENT_VARIANT[e.status]}>
          {t(`teacher.enrollmentStatus.${e.status}`)}
        </Badge>
        <span className="ms-auto text-xs text-content-muted">
          {t('teacher.studentDetail.enrolledOn')}: {formatDate(e.enrolledAt)}
        </span>
      </div>
      <div className="mt-1.5 flex flex-wrap gap-x-5 gap-y-1 text-xs text-content-secondary">
        <span>
          <span className="font-medium text-content-primary">{e.lessonsTotal}</span>
          {' '}{t('teacher.studentDetail.totalLessons')}
        </span>
        <span>
          <span className="font-medium text-content-primary">{e.lessonsCompleted}</span>
          {' '}{t('teacher.studentDetail.completed')}
        </span>
        <span>
          <span className="font-medium text-content-primary">{e.lessonsInProgress}</span>
          {' '}{t('teacher.studentDetail.inProgress')}
        </span>
        <span>
          {t('teacher.studentDetail.lastActivity')}:{' '}
          <span className="font-medium text-content-primary">
            {formatLastActivity(e.lastActivityAt)}
          </span>
        </span>
      </div>
    </div>
  )
}

function ProgressRow({ item: p }: { item: TeacherLessonProgressItemDto }) {
  const { t } = useTranslation()
  return (
    <div className="border-b border-stroke px-5 py-3 last:border-0">
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-sm font-medium text-content-primary">{p.lessonTitle}</span>
        <Badge variant={PROGRESS_VARIANT[p.status]}>
          {t(`teacher.progressStatus.${p.status}`)}
        </Badge>
        {p.scorePercent !== null && (
          <span className="ms-auto text-xs font-medium text-content-primary">
            {Math.round(p.scorePercent)}%
          </span>
        )}
      </div>
      <p className="mt-0.5 text-xs text-content-muted">
        {p.subjectName} · {p.chapterTitle}
        {p.completedAt && (
          <> · {t('teacher.studentDetail.completedOn')}: {formatDate(p.completedAt)}</>
        )}
      </p>
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

type Tab = 'enrollments' | 'progress'

/**
 * StudentMonitorPage — teacher read-only view of one assigned student.
 *
 * Two tabs:
 *   Enrollments — subject-level breakdown with lesson counts and last activity
 *   Progress    — lesson-level detail across active enrollments
 *
 * Access control is enforced server-side — a 403 response renders the
 * ErrorState (student not assigned to this teacher).
 *
 * Phase 2: add a performance trend chart below the progress table.
 * Phase 3: add teacher note input; add intervention flag.
 */
export default function StudentMonitorPage() {
  const { t }         = useTranslation()
  const navigate      = useNavigate()
  const { studentId } = useParams<{ studentId: string }>()

  const [activeTab, setActiveTab] = useState<Tab>('enrollments')

  const {
    data:      detail,
    isLoading: detailLoading,
    isError:   detailError,
    error:     detailErr,
    refetch:   refetchDetail,
  } = useStudentDetail(studentId)

  const {
    data:      progress,
    isLoading: progressLoading,
    isError:   progressError,
    error:     progressErr,
    refetch:   refetchProgress,
  } = useStudentProgress(activeTab === 'progress' ? studentId : undefined)

  const totalCompleted = detail?.enrollments.reduce((s, e) => s + e.lessonsCompleted, 0) ?? 0
  const activeCount    = detail?.enrollments.filter(e => e.status === 'Active').length ?? 0

  return (
    <PageContainer
      title={detail?.fullName ?? t('teacher.studentDetail.title')}
      description={detail?.gradeName ?? undefined}
      actions={
        <Button
          variant="secondary"
          size="sm"
          onClick={() => navigate('/teacher/students')}
        >
          ← {t('teacher.studentDetail.back')}
        </Button>
      }
    >
      <StateWrapper
        isLoading={detailLoading}
        isError={detailError}
        error={detailErr}
        isEmpty={false}
        onRetry={refetchDetail}
      >
        <div className="space-y-6">

          {/* ── Quick stats ──────────────────────────────────────────────── */}
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              label={t('teacher.studentDetail.grade')}
              value={detail?.gradeName ?? '—'}
            />
            <StatCard
              label={t('teacher.studentDetail.email')}
              value={detail?.email ?? '—'}
            />
            <StatCard
              label={t('teacher.studentDetail.activeEnrollments')}
              value={detailLoading ? '…' : String(activeCount)}
            />
            <StatCard
              label={t('teacher.studentDetail.totalCompleted')}
              value={detailLoading ? '…' : String(totalCompleted)}
            />
          </div>

          {/* ── Tab bar ──────────────────────────────────────────────────── */}
          <div className="flex gap-1 rounded-md border border-stroke bg-surface-raised p-1 w-fit">
            {(['enrollments', 'progress'] as Tab[]).map(tab => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                className={[
                  'rounded px-4 py-1.5 text-sm font-medium transition-colors',
                  activeTab === tab
                    ? 'bg-surface text-content-primary shadow-sm'
                    : 'text-content-secondary hover:text-content-primary',
                ].join(' ')}
              >
                {t(`teacher.studentDetail.tab.${tab}`)}
              </button>
            ))}
          </div>

          {/* ── Enrollments tab ──────────────────────────────────────────── */}
          {activeTab === 'enrollments' && (
            <SectionCard
              title={t('teacher.studentDetail.enrollments')}
              description={t('teacher.studentDetail.enrollmentsDesc')}
            >
              {!detail?.enrollments.length ? (
                <p className="px-5 py-4 text-sm text-content-muted">
                  {t('teacher.studentDetail.noEnrollments')}
                </p>
              ) : (
                detail.enrollments.map(e => (
                  <EnrollmentRow key={e.enrollmentId} enrollment={e} />
                ))
              )}
            </SectionCard>
          )}

          {/* ── Progress tab ─────────────────────────────────────────────── */}
          {activeTab === 'progress' && (
            <StateWrapper
              isLoading={progressLoading}
              isError={progressError}
              error={progressErr}
              isEmpty={!progress?.lessonProgress.length}
              emptyTitle={t('teacher.studentDetail.noProgress')}
              emptyDescription={t('teacher.studentDetail.noProgressDesc')}
              onRetry={refetchProgress}
            >
              <SectionCard
                title={t('teacher.studentDetail.lessonProgress')}
                description={t('teacher.studentDetail.lessonProgressDesc')}
              >
                {progress?.lessonProgress.map(p => (
                  <ProgressRow key={p.lessonId} item={p} />
                ))}
              </SectionCard>
            </StateWrapper>
          )}

        </div>
      </StateWrapper>
    </PageContainer>
  )
}
