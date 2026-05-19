import { useNavigate }   from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageContainer }  from '@/components/layout/PageContainer'
import { StateWrapper }   from '@/components/feedback'
import { Button }         from '@/components/ui'
import { useMyStudents }  from '@/features/teacher/hooks/useTeacher'

// ── Page ──────────────────────────────────────────────────────────────────────

/**
 * StudentsPage — lists all students assigned to the calling teacher.
 *
 * Each row shows: name, grade, active-enrollment count, and total lessons
 * completed.  A "View details" button navigates to
 * /teacher/students/{studentId} for the full enrollment + progress breakdown.
 *
 * Empty state: rendered when the teacher has no assigned students.
 * Error state: rendered on network/API failure with a retry action.
 *
 * Phase 2: add a search/filter bar; group by class/grade.
 * Phase 3: add at-risk indicators and quick-action links.
 */
export default function StudentsPage() {
  const { t }    = useTranslation()
  const navigate = useNavigate()

  const { data: students, isLoading, isError, error, refetch } = useMyStudents()

  return (
    <PageContainer
      title={t('teacher.students.title')}
      description={t('teacher.students.description')}
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!students?.length}
        emptyTitle={t('teacher.students.empty')}
        emptyDescription={t('teacher.students.emptyDescription')}
        onRetry={refetch}
      >
        <div className="overflow-hidden rounded-md border border-stroke bg-surface">
          <ul role="list" className="divide-y divide-stroke">
            {students?.map(s => (
              <li key={s.studentId} className="flex items-center gap-4 px-4 py-4">

                {/* Initials avatar */}
                <span
                  aria-hidden="true"
                  className="flex h-10 w-10 shrink-0 select-none items-center justify-center rounded-full bg-brand-100 text-sm font-bold uppercase text-brand-700"
                >
                  {s.fullName.charAt(0)}
                </span>

                {/* Info block */}
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold text-content-primary">
                    {s.fullName}
                  </p>
                  <p className="mt-0.5 text-xs text-content-secondary">
                    {s.gradeName ?? t('teacher.students.noGrade')}
                    {' · '}
                    {s.activeEnrollmentCount} {t('teacher.students.enrollments')}
                    {' · '}
                    {s.totalLessonsCompleted} {t('teacher.students.lessonsCompleted')}
                  </p>
                  <p className="mt-0.5 text-xs text-content-muted">
                    {s.email}
                  </p>
                </div>

                {/* Navigate to detail */}
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => navigate(`/teacher/students/${s.studentId}`)}
                >
                  {t('teacher.students.viewDetails')}
                </Button>
              </li>
            ))}
          </ul>
        </div>
      </StateWrapper>
    </PageContainer>
  )
}
