import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getStudents,
  getParentOptions,
  assignParent,
} from '@/services/student-admin.service'
import type { AssignParentPayload } from '@/types/student-admin'

// ── Query key factory ─────────────────────────────────────────────────────────

export const studentAdminKeys = {
  students:      ['admin', 'students', 'list'] as const,
  parentOptions: ['admin', 'students', 'parent-options'] as const,
}

// ── Queries ───────────────────────────────────────────────────────────────────

export function useStudents() {
  return useQuery({
    queryKey: studentAdminKeys.students,
    queryFn:  () => getStudents(),
  })
}

export function useParentOptions() {
  return useQuery({
    queryKey: studentAdminKeys.parentOptions,
    queryFn:  () => getParentOptions(),
  })
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useAssignParent() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ studentId, payload }: { studentId: string; payload: AssignParentPayload }) =>
      assignParent(studentId, payload),
    onSuccess: () => {
      // Refresh the students list so the updated linkage is reflected.
      qc.invalidateQueries({ queryKey: studentAdminKeys.students })
    },
  })
}
