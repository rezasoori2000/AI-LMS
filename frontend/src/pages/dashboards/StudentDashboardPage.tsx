import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { StateWrapper } from '@/components/feedback'
import { PageContainer } from '@/components/layout/PageContainer'
import { Badge, Button, SectionCard, StatCard } from '@/components/ui'
import { useStudentEnrollments, useStudentSummary } from '@/features/student/hooks/useStudent'

export default function StudentDashboardPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()

  const summaryQuery = useStudentSummary()
  const enrollmentsQuery = useStudentEnrollments()

  const isLoading = summaryQuery.isLoading || enrollmentsQuery.isLoading
  const isError = summaryQuery.isError || enrollmentsQuery.isError
  const error = summaryQuery.error ?? enrollmentsQuery.error

  const summary = summaryQuery.data
  const enrollments = enrollmentsQuery.data ?? []

  const continueLearning = enrollments
    .filter(item => item.nextLessonId)
    .slice(0, 3)

  return (
    <PageContainer
      title={t('dashboards.student.title')}
      description={t('dashboards.student.description')}
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={false}
        onRetry={() => {
          summaryQuery.refetch()
          enrollmentsQuery.refetch()
        }}
      >
        <div className="space-y-6">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              label={t('dashboards.student.stats.activeEnrollments')}
              value={summary?.activeEnrollments ?? 0}
            />
            <StatCard
              label={t('dashboards.student.stats.lessonsCompleted')}
              value={summary?.completedLessons ?? 0}
            />
            <StatCard
              label={t('dashboards.student.stats.inProgressLessons')}
              value={summary?.inProgressLessons ?? 0}
            />
            <StatCard
              label={t('dashboards.student.stats.overallProgress')}
              value={`${Math.round(summary?.overallProgressPercent ?? 0)}%`}
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-3">
            <div className="lg:col-span-2">
              <SectionCard
                title={t('dashboards.student.sections.continueLearning')}
                description="Resume from your next available lesson"
              >
                {!continueLearning.length ? (
                  <p className="text-sm text-content-secondary">
                  {t('dashboards.student.sections.continueLearningEmpty')}
                  </p>
                ) : (
                  <ul role="list" className="space-y-3">
                    {continueLearning.map(item => (
                      <li
                        key={item.enrollmentId}
                        className="flex flex-wrap items-center gap-3 rounded-md border border-stroke bg-surface-raised px-4 py-3"
                      >
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-medium text-content-primary">
                            {item.subjectName}
                          </p>
                          <p className="mt-0.5 text-xs text-content-secondary">
                            {item.completedLessons} of {item.totalLessons} lessons completed
                          </p>
                        </div>
                        <Button
                          size="sm"
                          onClick={() =>
                            navigate(`/student/subjects/${item.subjectId}/lessons/${item.nextLessonId}`)
                          }
                        >
                          {t('common.continue')}
                        </Button>
                      </li>
                    ))}
                  </ul>
                )}
              </SectionCard>
            </div>

            <SectionCard
              title={t('dashboards.student.sections.aiTutor')}
              description="Planned integration"
            >
              <p className="text-sm text-content-secondary">
                AI tutor support is deferred to a later phase. This MVP focuses on lesson access,
                answering, and completion tracking.
              </p>
              <Badge variant="info" className="mt-3">
                Deferred
              </Badge>
            </SectionCard>
          </div>

          <SectionCard
            title={t('dashboards.student.sections.mySubjects')}
            description="Open your enrollments and browse all available lessons"
            action={
              <Button size="sm" variant="secondary" onClick={() => navigate('/student/subjects')}>
                View all subjects
              </Button>
            }
          >
            {!enrollments.length ? (
              <p className="text-sm text-content-secondary">No active enrollments yet.</p>
            ) : (
              <ul role="list" className="space-y-2">
                {enrollments.slice(0, 5).map(item => (
                  <li
                    key={item.enrollmentId}
                    className="flex items-center justify-between gap-3 rounded-md border border-stroke bg-surface-raised px-4 py-2"
                  >
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium text-content-primary">{item.subjectName}</p>
                      <p className="text-xs text-content-secondary">
                        {item.completedLessons}/{item.totalLessons} lessons completed
                      </p>
                    </div>
                    <Badge variant="info">
                      {Math.round((item.totalLessons ? item.completedLessons / item.totalLessons : 0) * 100)}%
                    </Badge>
                  </li>
                ))}
              </ul>
            )}
          </SectionCard>
        </div>
      </StateWrapper>
    </PageContainer>
  )
}
