import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { InlineFeedback, StateWrapper } from '@/components/feedback'
import { PageContainer } from '@/components/layout/PageContainer'
import { Badge, Button, SectionCard } from '@/components/ui'
import { QuestionAnswerForm } from '@/features/student/components/QuestionAnswerForm'
import { useCompleteStudentLesson, useStartStudentLesson, useStudentLesson } from '@/features/student/hooks/useStudent'
import { TutorPanel } from '@/features/student/components/TutorPanel'

export default function LessonPlayerPage() {
  const navigate = useNavigate()
  const { subjectId, lessonId } = useParams<{ subjectId: string; lessonId: string }>()

  const [answers, setAnswers] = useState<Record<string, string>>({})

  const {
    data: lesson,
    isLoading,
    isError,
    error,
    refetch,
  } = useStudentLesson(lessonId)

  const completeLesson = useCompleteStudentLesson(lessonId)
  const startLesson = useStartStudentLesson()

  // Mark lesson InProgress when the player mounts. Fire-and-forget — a start
  // failure must never prevent the student from reading the lesson content.
  useEffect(() => {
    if (lessonId) {
      startLesson.mutate(lessonId)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lessonId])

  useEffect(() => {
    setAnswers({})
  }, [lessonId])

  const answeredCount = useMemo(() => {
    const total = lesson?.questions.length ?? 0
    if (total === 0) return 0

    return lesson?.questions.reduce((count, question) => {
      const value = (answers[question.questionId] ?? '').trim()
      return count + (value ? 1 : 0)
    }, 0) ?? 0
  }, [answers, lesson])

  function handleAnswerChange(questionId: string, answer: string) {
    setAnswers(prev => ({
      ...prev,
      [questionId]: answer,
    }))
  }

  async function handleSubmit() {
    if (!lessonId || !lesson) return

    const payload = {
      answers: lesson.questions
        .map(q => ({
          questionId: q.questionId,
          answer: (answers[q.questionId] ?? '').trim(),
        }))
        .filter(a => a.answer.length > 0),
    }

    await completeLesson.mutateAsync(payload)
  }

  return (
    <PageContainer
      title={lesson?.title ?? 'Lesson'}
      description={lesson ? `${lesson.subjectName} · ${lesson.chapterTitle}` : 'Lesson detail'}
      actions={
        <Button
          size="sm"
          variant="secondary"
          onClick={() => navigate(`/student/subjects/${subjectId}`)}
        >
          Back to lesson list
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
        <div className="space-y-4">
          <SectionCard
            title="Lesson content"
            description={lesson?.estimatedMinutes ? `${lesson.estimatedMinutes} min` : 'No duration estimate'}
            action={lesson ? <Badge variant="info">{lesson.progressStatus}</Badge> : null}
          >
            <div className="space-y-3">
              {lesson?.content ? (
                <div className="whitespace-pre-wrap text-sm leading-6 text-content-primary">
                  {lesson.content}
                </div>
              ) : (
                <p className="text-sm text-content-secondary">
                  No lesson text is available yet.
                </p>
              )}
            </div>
          </SectionCard>

          <SectionCard
            title="Questions"
            description={
              lesson?.questions.length
                ? `${answeredCount}/${lesson.questions.length} answered`
                : 'No questions for this lesson'
            }
          >
            {!lesson?.questions.length ? (
              <p className="text-sm text-content-secondary">
                This lesson has no questions. You can still submit completion.
              </p>
            ) : (
              <QuestionAnswerForm
                questions={lesson.questions}
                answers={answers}
                onAnswerChange={handleAnswerChange}
              />
            )}
          </SectionCard>

          <TutorPanel
            key={lessonId}
            lessonId={lessonId ?? ''}
            lessonTitle={lesson?.title ?? 'Lesson'}
          />

          {completeLesson.isError && (
            <InlineFeedback
              intent="error"
              message={
                completeLesson.error instanceof Error
                  ? completeLesson.error.message
                  : 'Could not submit lesson completion. Please try again.'
              }
            />
          )}

          {completeLesson.isSuccess && (
            <InlineFeedback
              intent="success"
              message={
                completeLesson.data.scorePercent !== null
                  ? `Submitted. Score: ${Math.round(completeLesson.data.scorePercent)}% (${completeLesson.data.correctCount}/${completeLesson.data.gradableQuestions}).`
                  : 'Submitted. Lesson completion was recorded.'
              }
            />
          )}

          <div className="flex flex-wrap gap-3">
            <Button isLoading={completeLesson.isPending} onClick={handleSubmit}>
              Submit lesson completion
            </Button>
            <p className="self-center text-xs text-content-muted">
              Correct answers are not revealed in Phase 1.
            </p>
          </div>
        </div>
      </StateWrapper>
    </PageContainer>
  )
}