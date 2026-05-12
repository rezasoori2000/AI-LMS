import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { PageContainer } from '@/components/layout/PageContainer'
import { Badge, Button } from '@/components/ui'
import { StateWrapper }  from '@/components/feedback'
import {
  useStudents,
  useParentOptions,
  useAssignParent,
} from '@/features/admin/students/hooks/useStudentAdmin'
import type { StudentLinkSummaryDto } from '@/types/student-admin'

/**
 * StudentsLinkPage — admin view for managing student–parent linkage.
 *
 * Shows all students with their current parent link status.
 * Inline row editing lets an admin assign or remove a parent.
 *
 * Phase 3 upgrade path:
 *  - Replace inline select with a searchable ComboBox when parent counts grow.
 *  - Add TenantId filter for SuperAdmin multi-tenant view.
 */
export default function StudentsLinkPage() {
  const { t } = useTranslation()

  const { data: students, isLoading, isError, error, refetch } = useStudents()
  const { data: parentOptions } = useParentOptions()
  const assignParentMutation = useAssignParent()

  // studentId → selected value in the inline edit form ('none' = unlink)
  const [editingId, setEditingId]   = useState<string | null>(null)
  const [selectValue, setSelectValue] = useState<string>('none')

  function startEdit(student: StudentLinkSummaryDto) {
    setEditingId(student.studentId)
    setSelectValue(student.parentProfileId ?? 'none')
  }

  function cancelEdit() {
    setEditingId(null)
    setSelectValue('none')
  }

  async function handleSave(studentId: string) {
    await assignParentMutation.mutateAsync({
      studentId,
      payload: { parentProfileId: selectValue === 'none' ? null : selectValue },
    })
    setEditingId(null)
  }

  return (
    <PageContainer
      title={t('admin.students.title')}
      description={t('admin.students.description')}
    >
      <StateWrapper
        isLoading={isLoading}
        isError={isError}
        error={error}
        isEmpty={!students?.length}
        emptyTitle={t('admin.students.empty')}
        emptyDescription={t('admin.students.emptyDescription')}
        onRetry={refetch}
      >
        <div className="overflow-hidden rounded-md border border-stroke bg-surface">
          {/* Header row */}
          <div className="hidden grid-cols-[1fr_10rem_1fr_10rem] gap-4 border-b border-stroke bg-surface-alt px-4 py-2 text-xs font-semibold uppercase tracking-wide text-content-secondary sm:grid">
            <span>{t('admin.students.colStudent')}</span>
            <span>{t('admin.students.colGrade')}</span>
            <span>{t('admin.students.colParent')}</span>
            <span className="text-right">{t('common.actions')}</span>
          </div>

          <ul role="list" className="divide-y divide-stroke">
            {students?.map(student => (
              <li key={student.studentId} className="px-4 py-3">
                {editingId === student.studentId ? (
                  /* ── Inline edit row ─────────────────────────────────── */
                  <div className="flex flex-wrap items-center gap-3">
                    <span className="flex-1 text-sm font-medium text-content-primary">
                      {student.fullName}
                    </span>

                    <select
                      className="rounded border border-stroke bg-surface px-2 py-1 text-sm text-content-primary focus:outline-none focus:ring-2 focus:ring-brand-500"
                      value={selectValue}
                      onChange={e => setSelectValue(e.target.value)}
                      aria-label={t('admin.students.selectParent')}
                    >
                      <option value="none">{t('admin.students.noParent')}</option>
                      {parentOptions?.map(opt => (
                        <option key={opt.parentProfileId} value={opt.parentProfileId}>
                          {opt.fullName} ({opt.email})
                        </option>
                      ))}
                    </select>

                    <div className="flex shrink-0 items-center gap-2">
                      <Button
                        size="sm"
                        onClick={() => handleSave(student.studentId)}
                        disabled={assignParentMutation.isPending}
                      >
                        {assignParentMutation.isPending
                          ? t('common.saving')
                          : t('common.save')}
                      </Button>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={cancelEdit}
                        disabled={assignParentMutation.isPending}
                      >
                        {t('common.cancel')}
                      </Button>
                    </div>
                  </div>
                ) : (
                  /* ── Display row ─────────────────────────────────────── */
                  <div className="grid grid-cols-1 gap-2 sm:grid-cols-[1fr_10rem_1fr_10rem] sm:items-center sm:gap-4">
                    <span className="text-sm font-medium text-content-primary">
                      {student.fullName}
                    </span>

                    <span className="text-sm text-content-secondary">
                      {student.gradeName ?? (
                        <span className="italic text-content-tertiary">
                          {t('admin.students.noGrade')}
                        </span>
                      )}
                    </span>

                    <span className="flex items-center gap-2 text-sm">
                      {student.parentProfileId ? (
                        <>
                          <span className="text-content-primary">
                            {student.parentFullName}
                          </span>
                          <span className="text-xs text-content-secondary">
                            {student.parentEmail}
                          </span>
                        </>
                      ) : (
                        <Badge variant="warning">
                          {t('admin.students.unlinked')}
                        </Badge>
                      )}
                    </span>

                    <div className="flex justify-end">
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => startEdit(student)}
                      >
                        {student.parentProfileId
                          ? t('admin.students.changeParent')
                          : t('admin.students.assignParent')}
                      </Button>
                    </div>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </div>
      </StateWrapper>
    </PageContainer>
  )
}
