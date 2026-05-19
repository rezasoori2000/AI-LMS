import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { PageContainer } from '@/components/layout/PageContainer'
import { Badge, Button } from '@/components/ui'
import { StateWrapper }  from '@/components/feedback'
import {
  useStudents,
  useParentOptions,
  useAssignParent,
  useTeacherOptions,
  useAssignTeacher,
} from '@/features/admin/students/hooks/useStudentAdmin'
import type { StudentLinkSummaryDto } from '@/types/student-admin'

/**
 * StudentsLinkPage — admin view for managing student–parent and student–teacher linkage.
 *
 * Shows all students with their current parent and teacher assignment status.
 * Inline row editing lets an admin assign or remove a parent or teacher.
 *
 * Phase 3 upgrade path:
 *  - Replace inline select with a searchable ComboBox when option counts grow.
 *  - Add TenantId filter for SuperAdmin multi-tenant view.
 */

type EditField = 'parent' | 'teacher'

interface EditState {
  studentId: string
  field:     EditField
  value:     string  // parentProfileId | teacherUserId | 'none'
}

export default function StudentsLinkPage() {
  const { t } = useTranslation()

  const { data: students, isLoading, isError, error, refetch } = useStudents()
  const { data: parentOptions  } = useParentOptions()
  const { data: teacherOptions } = useTeacherOptions()
  const assignParentMutation  = useAssignParent()
  const assignTeacherMutation = useAssignTeacher()

  const [editState, setEditState] = useState<EditState | null>(null)

  function startEdit(student: StudentLinkSummaryDto, field: EditField) {
    const value = field === 'parent'
      ? (student.parentProfileId ?? 'none')
      : (student.teacherUserId   ?? 'none')
    setEditState({ studentId: student.studentId, field, value })
  }

  function cancelEdit() {
    setEditState(null)
  }

  async function handleSave(studentId: string) {
    if (!editState) return
    if (editState.field === 'parent') {
      await assignParentMutation.mutateAsync({
        studentId,
        payload: { parentProfileId: editState.value === 'none' ? null : editState.value },
      })
    } else {
      await assignTeacherMutation.mutateAsync({
        studentId,
        payload: { teacherUserId: editState.value === 'none' ? null : editState.value },
      })
    }
    setEditState(null)
  }

  const isSaving = assignParentMutation.isPending || assignTeacherMutation.isPending

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
          <div className="hidden grid-cols-[1fr_8rem_1fr_1fr_10rem] gap-4 border-b border-stroke bg-surface-alt px-4 py-2 text-xs font-semibold uppercase tracking-wide text-content-secondary sm:grid">
            <span>{t('admin.students.colStudent')}</span>
            <span>{t('admin.students.colGrade')}</span>
            <span>{t('admin.students.colParent')}</span>
            <span>{t('admin.students.colTeacher')}</span>
            <span className="text-right">{t('common.actions')}</span>
          </div>

          <ul role="list" className="divide-y divide-stroke">
            {students?.map(student => (
              <li key={student.studentId} className="px-4 py-3">
                {editState?.studentId === student.studentId ? (
                  /* ── Inline edit row ─────────────────────────────────── */
                  <div className="flex flex-wrap items-center gap-3">
                    <span className="flex-1 text-sm font-medium text-content-primary">
                      {student.fullName}
                    </span>

                    {editState.field === 'parent' ? (
                      <select
                        className="rounded border border-stroke bg-surface px-2 py-1 text-sm text-content-primary focus:outline-none focus:ring-2 focus:ring-brand-500"
                        value={editState.value}
                        onChange={e => setEditState({ ...editState, value: e.target.value })}
                        aria-label={t('admin.students.selectParent')}
                      >
                        <option value="none">{t('admin.students.noParent')}</option>
                        {parentOptions?.map(opt => (
                          <option key={opt.parentProfileId} value={opt.parentProfileId}>
                            {opt.fullName} ({opt.email})
                          </option>
                        ))}
                      </select>
                    ) : (
                      <select
                        className="rounded border border-stroke bg-surface px-2 py-1 text-sm text-content-primary focus:outline-none focus:ring-2 focus:ring-brand-500"
                        value={editState.value}
                        onChange={e => setEditState({ ...editState, value: e.target.value })}
                        aria-label={t('admin.students.selectTeacher')}
                      >
                        <option value="none">{t('admin.students.noTeacher')}</option>
                        {teacherOptions?.map(opt => (
                          <option key={opt.teacherUserId} value={opt.teacherUserId}>
                            {opt.fullName} ({opt.email})
                          </option>
                        ))}
                      </select>
                    )}

                    <div className="flex shrink-0 items-center gap-2">
                      <Button
                        size="sm"
                        onClick={() => handleSave(student.studentId)}
                        disabled={isSaving}
                      >
                        {isSaving ? t('common.saving') : t('common.save')}
                      </Button>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={cancelEdit}
                        disabled={isSaving}
                      >
                        {t('common.cancel')}
                      </Button>
                    </div>
                  </div>
                ) : (
                  /* ── Display row ─────────────────────────────────────── */
                  <div className="grid grid-cols-1 gap-2 sm:grid-cols-[1fr_8rem_1fr_1fr_10rem] sm:items-center sm:gap-4">
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
                          <span className="text-content-primary">{student.parentFullName}</span>
                          <span className="text-xs text-content-secondary">{student.parentEmail}</span>
                        </>
                      ) : (
                        <Badge variant="warning">{t('admin.students.unlinked')}</Badge>
                      )}
                    </span>

                    <span className="flex items-center gap-2 text-sm">
                      {student.teacherUserId ? (
                        <span className="text-content-primary">{student.teacherFullName}</span>
                      ) : (
                        <Badge variant="warning">{t('admin.students.unassignedTeacher')}</Badge>
                      )}
                    </span>

                    <div className="flex flex-col items-end gap-1">
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => startEdit(student, 'parent')}
                      >
                        {student.parentProfileId
                          ? t('admin.students.changeParent')
                          : t('admin.students.assignParent')}
                      </Button>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => startEdit(student, 'teacher')}
                      >
                        {student.teacherUserId
                          ? t('admin.students.changeTeacher')
                          : t('admin.students.assignTeacher')}
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

