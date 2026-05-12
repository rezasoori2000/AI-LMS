import { useNavigate }     from 'react-router-dom'
import { useTranslation }  from 'react-i18next'
import { PageContainer }      from '@/components/layout/PageContainer'
import { Button }             from '@/components/ui'
import { StateWrapper }       from '@/components/feedback'
import { ConfirmDeleteButton } from '@/components/admin/ConfirmDeleteButton'
import { useLessons, useDeleteLesson } from '@/features/admin/content/hooks/useLessons'
import { useChapters } from '@/features/admin/content/hooks/useChapters'

/**
 * LessonsPage — admin list view for lessons.
 *
 * Lessons are returned ordered by order (ascending, per chapter).
 * Chapter names are resolved from a parallel query for display.
 *
 * Phase 2: add chapter filter dropdown above the list.
 * Phase 2: add pagination when lesson counts grow.
 * Phase 3: add lesson content preview / status indicator.
 */
export default function LessonsPage() {
  const { t }    = useTranslation()
  const navigate = useNavigate()

  const { data: lessons, isLoading, isError, error, refetch } = useLessons()
  const { data: chapters = [] } = useChapters()
  const deleteMutation          = useDeleteLesson()

  const chapterMap = Object.fromEntries(chapters.map(c => [c.id, c.title]))

  return (
    <PageContainer
      title={t('admin.content.lessons.title')}
      description={t('admin.content.lessons.description')}
      actions={
        <Button size="sm" onClick={() => navigate('/admin/content/lessons/new')}>
          {t('admin.content.lessons.new')}
        </Button>
      }
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!lessons?.length}
        emptyTitle={t('admin.content.lessons.empty')}
        emptyDescription={t('admin.content.lessons.emptyDescription')}
        emptyAction={
          <Button size="sm" onClick={() => navigate('/admin/content/lessons/new')}>
            {t('admin.content.lessons.new')}
          </Button>
        }
        onRetry={refetch}
      >
        <div className="overflow-hidden rounded-md border border-stroke bg-surface">
          <ul role="list" className="divide-y divide-stroke">
            {lessons?.map(lesson => (
              <li key={lesson.id} className="flex items-center gap-4 px-4 py-3">
                {/* Order badge */}
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-surface-raised text-xs font-semibold text-content-secondary">
                  {lesson.order}
                </span>

                {/* Title + chapter + duration */}
                <div className="flex-1 min-w-0">
                  <p className="truncate text-sm font-medium text-content-primary">
                    {lesson.title}
                  </p>
                  <p className="truncate text-xs text-content-muted">
                    {chapterMap[lesson.chapterId] ?? '—'}
                    {lesson.estimatedMinutes != null && (
                      <>&nbsp;·&nbsp;{lesson.estimatedMinutes} {t('admin.content.lessons.minutesShort')}</>
                    )}
                  </p>
                </div>

                {/* Actions */}
                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => navigate(`/admin/content/questions?lessonId=${lesson.id}`)}
                  >
                    {t('admin.content.lessons.questions')}
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => navigate(`/admin/content/lessons/${lesson.id}/edit`)}
                  >
                    {t('common.edit')}
                  </Button>
                  <ConfirmDeleteButton
                    onConfirm={() => deleteMutation.mutate(lesson.id)}
                    isLoading={deleteMutation.isPending && deleteMutation.variables === lesson.id}
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
