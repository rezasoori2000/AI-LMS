import { useNavigate }     from 'react-router-dom'
import { useTranslation }  from 'react-i18next'
import { PageContainer }      from '@/components/layout/PageContainer'
import { Button }             from '@/components/ui'
import { StateWrapper }       from '@/components/feedback'
import { ConfirmDeleteButton } from '@/components/admin/ConfirmDeleteButton'
import { useChapters, useDeleteChapter } from '@/features/admin/content/hooks/useChapters'
import { useGrades }   from '@/features/admin/content/hooks/useGrades'
import { useSubjects } from '@/features/admin/content/hooks/useSubjects'

/**
 * ChaptersPage — admin list view for chapters.
 *
 * Chapters are linked to both a Subject and a Grade.
 * Both lists are fetched in parallel and resolved client-side for display,
 * which is acceptable at Phase 1 scale.
 *
 * Phase 2: add subject/grade filter dropdowns above the list.
 * Phase 2: add pagination when chapter counts grow.
 */
export default function ChaptersPage() {
  const { t }    = useTranslation()
  const navigate = useNavigate()

  const { data: chapters, isLoading, isError, error, refetch } = useChapters()
  const { data: grades   = [] } = useGrades()
  const { data: subjects = [] } = useSubjects()
  const deleteMutation          = useDeleteChapter()

  // Build lookup maps for display — only computed once per render
  const gradeMap   = Object.fromEntries(grades.map(g => [g.id, g.name]))
  const subjectMap = Object.fromEntries(subjects.map(s => [s.id, s.name]))

  return (
    <PageContainer
      title={t('admin.content.chapters.title')}
      description={t('admin.content.chapters.description')}
      actions={
        <Button size="sm" onClick={() => navigate('/admin/content/chapters/new')}>
          {t('admin.content.chapters.new')}
        </Button>
      }
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!chapters?.length}
        emptyTitle={t('admin.content.chapters.empty')}
        emptyDescription={t('admin.content.chapters.emptyDescription')}
        emptyAction={
          <Button size="sm" onClick={() => navigate('/admin/content/chapters/new')}>
            {t('admin.content.chapters.new')}
          </Button>
        }
        onRetry={refetch}
      >
        <div className="overflow-hidden rounded-md border border-stroke bg-surface">
          <ul role="list" className="divide-y divide-stroke">
            {chapters?.map(chapter => (
              <li key={chapter.id} className="flex items-center gap-4 px-4 py-3">
                {/* Order badge */}
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-surface-raised text-xs font-semibold text-content-secondary">
                  {chapter.order}
                </span>

                {/* Title + subject · grade breadcrumb */}
                <div className="flex-1 min-w-0">
                  <p className="truncate text-sm font-medium text-content-primary">
                    {chapter.title}
                  </p>
                  <p className="truncate text-xs text-content-muted">
                    {subjectMap[chapter.subjectId] ?? '—'}
                    &nbsp;·&nbsp;
                    {gradeMap[chapter.gradeId] ?? '—'}
                  </p>
                </div>

                {/* Actions */}
                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => navigate(`/admin/content/chapters/${chapter.id}/edit`)}
                  >
                    {t('common.edit')}
                  </Button>
                  <ConfirmDeleteButton
                    onConfirm={() => deleteMutation.mutate(chapter.id)}
                    isLoading={deleteMutation.isPending && deleteMutation.variables === chapter.id}
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
