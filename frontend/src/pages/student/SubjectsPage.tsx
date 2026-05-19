import { useNavigate } from 'react-router-dom'
import { PageContainer } from '@/components/layout/PageContainer'
import { StateWrapper } from '@/components/feedback'
import { Badge, Button, SectionCard } from '@/components/ui'
import { useStudentEnrollments } from '@/features/student/hooks/useStudent'

function enrollmentPercent(completed: number, total: number): number {
  if (total <= 0) return 0
  return Math.round((completed / total) * 100)
}

export default function SubjectsPage() {
  const navigate = useNavigate()

  const {
    data: enrollments,
    isLoading,
    isError,
    error,
    refetch,
  } = useStudentEnrollments()

  return (
    <PageContainer
      title="My Subjects"
      description="Browse your enrolled subjects and continue learning."
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!enrollments?.length}
        emptyTitle="No active enrollments"
        emptyDescription="You are not enrolled in any subjects yet."
        onRetry={refetch}
      >
        <div className="grid gap-4 md:grid-cols-2">
          {enrollments?.map(subject => {
            const percent = enrollmentPercent(subject.completedLessons, subject.totalLessons)

            return (
              <SectionCard
                key={subject.enrollmentId}
                title={subject.subjectName}
                description={subject.gradeName ?? 'No grade assigned'}
                action={<Badge variant="info">{percent}%</Badge>}
              >
                <div className="space-y-3">
                  <p className="text-sm text-content-secondary">
                    {subject.completedLessons} of {subject.totalLessons} lessons completed.
                  </p>

                  <div className="flex flex-wrap gap-2">
                    <Button
                      size="sm"
                      onClick={() => navigate(`/student/subjects/${subject.subjectId}`)}
                    >
                      Open lessons
                    </Button>

                    {subject.nextLessonId && (
                      <Button
                        size="sm"
                        variant="secondary"
                        onClick={() =>
                          navigate(`/student/subjects/${subject.subjectId}/lessons/${subject.nextLessonId}`)
                        }
                      >
                        Continue next lesson
                      </Button>
                    )}
                  </div>
                </div>
              </SectionCard>
            )
          })}
        </div>
      </StateWrapper>
    </PageContainer>
  )
}