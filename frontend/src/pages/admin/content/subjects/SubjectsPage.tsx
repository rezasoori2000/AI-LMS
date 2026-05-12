import { useNavigate }     from 'react-router-dom'
import { useTranslation }  from 'react-i18next'
import { PageContainer }      from '@/components/layout/PageContainer'
import { Button }             from '@/components/ui'
import { StateWrapper }       from '@/components/feedback'
import { ConfirmDeleteButton } from '@/components/admin/ConfirmDeleteButton'
import { useSubjects, useDeleteSubject } from '@/features/admin/content/hooks/useSubjects'

/**
 * SubjectsPage — admin list view for subjects.
 *
 * Subjects are returned alphabetically by name from the API.
 * Phase 2: add pagination when subject counts grow.
 */
export default function SubjectsPage() {
  const { t }    = useTranslation()
  const navigate = useNavigate()
  const { data: subjects, isLoading, isError, error, refetch } = useSubjects()
  const deleteMutation = useDeleteSubject()

  return (
    <PageContainer
      title={t('admin.content.subjects.title')}
      description={t('admin.content.subjects.description')}
      actions={
        <Button size="sm" onClick={() => navigate('/admin/content/subjects/new')}>
          {t('admin.content.subjects.new')}
        </Button>
      }
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!subjects?.length}
        emptyTitle={t('admin.content.subjects.empty')}
        emptyDescription={t('admin.content.subjects.emptyDescription')}
        emptyAction={
          <Button size="sm" onClick={() => navigate('/admin/content/subjects/new')}>
            {t('admin.content.subjects.new')}
          </Button>
        }
        onRetry={refetch}
      >
        <div className="overflow-hidden rounded-md border border-stroke bg-surface">
          <ul role="list" className="divide-y divide-stroke">
            {subjects?.map(subject => (
              <li key={subject.id} className="flex items-center gap-4 px-4 py-3">
                {/* Name + slug */}
                <div className="flex-1 min-w-0">
                  <p className="truncate text-sm font-medium text-content-primary">
                    {subject.name}
                  </p>
                  <p className="truncate text-xs text-content-muted font-mono">
                    {subject.slug}
                  </p>
                </div>

                {/* Description (if present) */}
                {subject.description && (
                  <p className="hidden md:block flex-1 truncate text-sm text-content-secondary">
                    {subject.description}
                  </p>
                )}

                {/* Actions */}
                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => navigate(`/admin/content/subjects/${subject.id}/edit`)}
                  >
                    {t('common.edit')}
                  </Button>
                  <ConfirmDeleteButton
                    onConfirm={() => deleteMutation.mutate(subject.id)}
                    isLoading={deleteMutation.isPending && deleteMutation.variables === subject.id}
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
