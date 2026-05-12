import { useNavigate, useSearchParams } from 'react-router-dom'
import { useTranslation }               from 'react-i18next'
import { PageContainer }                from '@/components/layout/PageContainer'
import { Button, Badge }                from '@/components/ui'
import { StateWrapper }                 from '@/components/feedback'
import { ConfirmDeleteButton }          from '@/components/admin/ConfirmDeleteButton'
import {
  useQuestions,
  useDeleteQuestion,
} from '@/features/admin/content/hooks/useQuestions'
import { useLesson } from '@/features/admin/content/hooks/useLessons'
import type { QuestionType, DifficultyLevel } from '@/types/content'

// ── Badge helpers ─────────────────────────────────────────────────────────────

const TYPE_VARIANT: Record<QuestionType, 'info' | 'success' | 'default'> = {
  MultipleChoice: 'info',
  TrueFalse:      'success',
  ShortAnswer:    'default',
}

const DIFFICULTY_VARIANT: Record<DifficultyLevel, 'success' | 'warning' | 'error'> = {
  Easy:   'success',
  Medium: 'warning',
  Hard:   'error',
}

/**
 * QuestionsPage — admin list view for the question bank.
 *
 * Supports an optional `?lessonId=` query param to show only questions
 * attached to a specific lesson.  The LessonsPage links here with that
 * param, giving admins a fast in-context question management flow.
 *
 * Phase 2: add filters for type and difficulty above the list.
 * Phase 2: add pagination when question counts grow.
 * Phase 2: replace lesson select in the form with a searchable combobox.
 * Phase 3: show question preview (options, correct answer) inline.
 */
export default function QuestionsPage() {
  const { t }      = useTranslation()
  const navigate   = useNavigate()
  const [params]   = useSearchParams()
  const lessonId   = params.get('lessonId') ?? undefined

  const { data: questions, isLoading, isError, error, refetch } =
    useQuestions(lessonId)
  const { data: contextLesson } = useLesson(lessonId)
  const deleteMutation          = useDeleteQuestion()

  const newHref = lessonId
    ? `/admin/content/questions/new?lessonId=${lessonId}`
    : '/admin/content/questions/new'

  return (
    <PageContainer
      title={t('admin.content.questions.title')}
      description={
        contextLesson
          ? `${t('admin.content.questions.filteringByLesson')}: ${contextLesson.title}`
          : t('admin.content.questions.description')
      }
      actions={
        <div className="flex items-center gap-2">
          {lessonId && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/admin/content/questions')}
            >
              {t('admin.content.questions.viewAll')}
            </Button>
          )}
          <Button size="sm" onClick={() => navigate(newHref)}>
            {t('admin.content.questions.new')}
          </Button>
        </div>
      }
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!questions?.length}
        emptyTitle={t('admin.content.questions.empty')}
        emptyDescription={
          lessonId
            ? t('admin.content.questions.emptyLessonDescription')
            : t('admin.content.questions.emptyDescription')
        }
        emptyAction={
          <Button size="sm" onClick={() => navigate(newHref)}>
            {t('admin.content.questions.new')}
          </Button>
        }
        onRetry={refetch}
      >
        <div className="overflow-hidden rounded-md border border-stroke bg-surface">
          <ul role="list" className="divide-y divide-stroke">
            {questions?.map(question => (
              <li key={question.id} className="flex items-start gap-4 px-4 py-3">
                {/* Type + difficulty badges */}
                <div className="flex shrink-0 flex-col gap-1 pt-0.5">
                  <Badge variant={TYPE_VARIANT[question.type]}>
                    {t(`admin.content.questions.types.${question.type}`)}
                  </Badge>
                  <Badge variant={DIFFICULTY_VARIANT[question.difficulty]}>
                    {t(`admin.content.questions.difficulty.${question.difficulty}`)}
                  </Badge>
                </div>

                {/* Question text */}
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-content-primary line-clamp-2">
                    {question.text}
                  </p>
                  {question.lessonId && !lessonId && (
                    <p className="mt-0.5 text-xs text-content-muted">
                      {t('admin.content.questions.attachedToLesson')}
                    </p>
                  )}
                  {!question.lessonId && (
                    <p className="mt-0.5 text-xs text-content-muted">
                      {t('admin.content.questions.standaloneBankItem')}
                    </p>
                  )}
                </div>

                {/* Actions */}
                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => navigate(`/admin/content/questions/${question.id}/edit`)}
                  >
                    {t('common.edit')}
                  </Button>
                  <ConfirmDeleteButton
                    onConfirm={() => deleteMutation.mutate(question.id)}
                    isLoading={
                      deleteMutation.isPending &&
                      deleteMutation.variables === question.id
                    }
                    label={t('common.delete')}
                  />
                </div>
              </li>
            ))}
          </ul>
        </div>
      </StateWrapper>
    </PageContainer>
  )
}
