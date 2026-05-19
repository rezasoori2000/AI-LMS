import { useNavigate }   from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageContainer }  from '@/components/layout/PageContainer'
import { StateWrapper }   from '@/components/feedback'
import { StatCard, SectionCard, Button } from '@/components/ui'
import { useTeacherSummary, useMyStudents } from '@/features/teacher/hooks/useTeacher'

/**
 * Teacher Dashboard — live stat row + assigned-student overview.
 *
 * Connected (Phase 1, Section 8):
 *   - Stat row: assigned students, active enrollments, completions this week
 *   - Student Overview: assigned-student list with quick navigate to detail
 *
 * Placeholder sections (Phase 2+):
 *   - Upcoming sessions / timetable
 *   - Flagged / at-risk learners
 *   - Open assignments and recent submissions
 */
export default function TeacherDashboardPage() {
  const { t }    = useTranslation()
  const navigate = useNavigate()

  const { data: summary,  isLoading: summaryLoading  } = useTeacherSummary()
  const { data: students, isLoading: studentsLoading,
          isError,        refetch                     } = useMyStudents()

  return (
    <PageContainer
      title={t('dashboards.teacher.title')}
      description={t('dashboards.teacher.description')}
    >
      <div className="space-y-6">
        {/* ── Stat row ──────────────────────────────────────────────────── */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <StatCard
            label={t('dashboards.teacher.stats.students')}
            value={summaryLoading ? '…' : String(summary?.totalAssignedStudents ?? 0)}
          />
          <StatCard
            label={t('dashboards.teacher.stats.activeEnrollments')}
            value={summaryLoading ? '…' : String(summary?.activeEnrollments ?? 0)}
          />
          <StatCard
            label={t('dashboards.teacher.stats.completedThisWeek')}
            value={summaryLoading ? '…' : String(summary?.lessonsCompletedThisWeek ?? 0)}
          />
        </div>

        {/* ── Assigned students overview ───────────────────────────────── */}
        <SectionCard
          title={t('teacher.students.title')}
          description={t('teacher.students.description')}
          action={
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/teacher/students')}
            >
              {t('teacher.students.viewAll')}
            </Button>
          }
        >
          <StateWrapper
            isLoading={studentsLoading}
            isError={isError}
            isEmpty={!students?.length}
            emptyTitle={t('teacher.students.empty')}
            emptyDescription={t('teacher.students.emptyDescription')}
            onRetry={refetch}
          >
            <ul className="-mx-5 -mb-4 divide-y divide-stroke" role="list">
              {students?.slice(0, 5).map(s => (
                <li
                  key={s.studentId}
                  className="flex cursor-pointer items-center gap-3 px-5 py-3 hover:bg-surface-raised transition-colors"
                  onClick={() => navigate(`/teacher/students/${s.studentId}`)}
                >
                  <span
                    aria-hidden="true"
                    className="flex h-9 w-9 shrink-0 select-none items-center justify-center rounded-full bg-brand-100 text-sm font-bold uppercase text-brand-700"
                  >
                    {s.fullName.charAt(0)}
                  </span>
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
                  </div>
                  <span className="text-xs text-content-muted">
                    {t('teacher.students.viewDetails')} →
                  </span>
                </li>
              ))}
            </ul>
          </StateWrapper>
        </SectionCard>
      </div>
    </PageContainer>
  )
}
