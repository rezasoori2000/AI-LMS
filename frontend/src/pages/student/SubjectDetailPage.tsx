import { useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageContainer } from '@/components/layout/PageContainer'
import { StateWrapper } from '@/components/feedback'
import { Badge, Button, SectionCard } from '@/components/ui'
import { useStudentSubjectChapters } from '@/features/student/hooks/useStudent'
import type { ProgressStatus } from '@/types/student'

const STATUS_VARIANT: Record<ProgressStatus, 'default' | 'info' | 'success'> = {
  NotStarted: 'default',
  InProgress: 'info',
  Completed: 'success',
}

export default function SubjectDetailPage() {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const { subjectId } = useParams<{ subjectId: string }>()

  const { data, isLoading, isError, error, refetch } = useStudentSubjectChapters(subjectId)

  const lessonCount = useMemo(
    () => data?.chapters.reduce((sum, chapter) => sum + chapter.lessons.length, 0) ?? 0,
    [data],
  )

  return (
    <PageContainer
      title={data?.subjectName ?? 'Subject lessons'}
      description={lessonCount ? `${lessonCount} lessons available` : 'Lesson list by chapter'}
      actions={
        <Button size="sm" variant="secondary" onClick={() => navigate('/student/subjects')}>
          Back to subjects
        </Button>
      }
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!data?.chapters.length || lessonCount === 0}
        emptyTitle="No lessons in this subject yet"
        emptyDescription="Your teacher or admin has not published lessons for this subject yet."
        onRetry={refetch}
      >
        <div className="space-y-4">
          {data?.chapters.map(chapter => (
            <SectionCard
              key={chapter.chapterId}
              title={chapter.chapterTitle}
              description={`Chapter ${chapter.order}`}
            >
              {!chapter.lessons.length ? (
                <p className="text-sm text-content-secondary">No lessons in this chapter yet.</p>
              ) : (
                <ul role="list" className="space-y-3">
                  {chapter.lessons.map(lesson => (
                    <li
                      key={lesson.lessonId}
                      className="flex flex-wrap items-center gap-3 rounded-md border border-stroke bg-surface-raised px-4 py-3"
                    >
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium text-content-primary">
                          {lesson.title}
                        </p>
                        <p className="mt-0.5 text-xs text-content-secondary">
                          Lesson {lesson.order}
                          {lesson.estimatedMinutes ? ` · ${lesson.estimatedMinutes} min` : ''}
                        </p>
                      </div>

                      <Badge variant={STATUS_VARIANT[lesson.progressStatus]}>
                        {t(`dashboards.student.progress.${lesson.progressStatus}`)}
                      </Badge>

                      <Button
                        size="sm"
                        onClick={() => navigate(`/student/subjects/${data.subjectId}/lessons/${lesson.lessonId}`)}
                      >
                        Open lesson
                      </Button>
                    </li>
                  ))}
                </ul>
              )}
            </SectionCard>
          ))}
        </div>
      </StateWrapper>
    </PageContainer>
  )
}