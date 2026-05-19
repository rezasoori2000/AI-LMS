import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getStudents,
  getParentOptions,
  assignParent,
  getTeacherOptions,
  assignTeacher,
} from '@/services/student-admin.service'
import type { AssignParentPayload, AssignTeacherPayload } from '@/types/student-admin'

// ── Query key factory ─────────────────────────────────────────────────────────

export const studentAdminKeys = {
  students:       ['admin', 'students', 'list'] as const,
  parentOptions:  ['admin', 'students', 'parent-options'] as const,
  teacherOptions: ['admin', 'students', 'teacher-options'] as const,
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

export function useTeacherOptions() {
  return useQuery({
    queryKey: studentAdminKeys.teacherOptions,
    queryFn:  () => getTeacherOptions(),
  })
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useAssignParent() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ studentId, payload }: { studentId: string; payload: AssignParentPayload }) =>
      assignParent(studentId, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: studentAdminKeys.students })
    },
  })
}

export function useAssignTeacher() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ studentId, payload }: { studentId: string; payload: AssignTeacherPayload }) =>
      assignTeacher(studentId, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: studentAdminKeys.students })
    },
  })
}
