import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageContainer }       from '@/components/layout/PageContainer'
import { Button }              from '@/components/ui'
import { StateWrapper }        from '@/components/feedback'
import { ConfirmDeleteButton }  from '@/components/admin/ConfirmDeleteButton'
import { useGrades, useDeleteGrade } from '@/features/admin/content/hooks/useGrades'

/**
 * GradesPage — admin list view for grade levels.
 *
 * Displays all grades ordered by level (ascending, as returned by the API).
 * Provides New, Edit, and Delete actions per row.
 *
 * Phase 2: add pagination + sort controls when grade counts grow.
 * Phase 3: add tenant filter for SuperAdmin users.
 */
export default function GradesPage() {
  const { t }    = useTranslation()
  const navigate = useNavigate()
  const { data: grades, isLoading, isError, error, refetch } = useGrades()
  const deleteMutation = useDeleteGrade()

  return (
    <PageContainer
      title={t('admin.content.grades.title')}
      description={t('admin.content.grades.description')}
      actions={
        <Button size="sm" onClick={() => navigate('/admin/content/grades/new')}>
          {t('admin.content.grades.new')}
        </Button>
      }
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!grades?.length}
        emptyTitle={t('admin.content.grades.empty')}
        emptyDescription={t('admin.content.grades.emptyDescription')}
        emptyAction={
          <Button size="sm" onClick={() => navigate('/admin/content/grades/new')}>
            {t('admin.content.grades.new')}
          </Button>
        }
        onRetry={refetch}
      >
        <div className="overflow-hidden rounded-md border border-stroke bg-surface">
          <ul role="list" className="divide-y divide-stroke">
            {grades?.map(grade => (
              <li key={grade.id} className="flex items-center gap-4 px-4 py-3">
                {/* Level badge */}
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-brand-100 text-xs font-bold text-brand-700">
                  {grade.level}
                </span>

                {/* Name */}
                <span className="flex-1 text-sm font-medium text-content-primary">
                  {grade.name}
                </span>

                {/* Actions */}
                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => navigate(`/admin/content/grades/${grade.id}/edit`)}
                  >
                    {t('common.edit')}
                  </Button>
                  <ConfirmDeleteButton
                    onConfirm={() => deleteMutation.mutate(grade.id)}
                    isLoading={deleteMutation.isPending && deleteMutation.variables === grade.id}
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
