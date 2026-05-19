import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getStudentSummary,
  getStudentEnrollments,
  getStudentSubjectChapters,
  getStudentLesson,
  completeStudentLesson,
  startStudentLesson,
} from '@/services/student.service'
import type { CompleteRequest } from '@/types/student'

export const studentKeys = {
  root: ['student'] as const,
  summary: ['student', 'summary'] as const,
  enrollments: ['student', 'enrollments'] as const,
  subjectChapters: (subjectId: string) => ['student', 'subjects', subjectId, 'chapters'] as const,
  lessonDetail: (lessonId: string) => ['student', 'lessons', lessonId] as const,
}

export function useStudentSummary() {
  return useQuery({
    queryKey: studentKeys.summary,
    queryFn: getStudentSummary,
  })
}

export function useStudentEnrollments() {
  return useQuery({
    queryKey: studentKeys.enrollments,
    queryFn: getStudentEnrollments,
  })
}

export function useStudentSubjectChapters(subjectId: string | undefined) {
  return useQuery({
    queryKey: studentKeys.subjectChapters(subjectId ?? ''),
    queryFn: () => getStudentSubjectChapters(subjectId!),
    enabled: Boolean(subjectId),
  })
}

export function useStudentLesson(lessonId: string | undefined) {
  return useQuery({
    queryKey: studentKeys.lessonDetail(lessonId ?? ''),
    queryFn: () => getStudentLesson(lessonId!),
    enabled: Boolean(lessonId),
  })
}

export function useCompleteStudentLesson(lessonId: string | undefined) {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (payload: CompleteRequest) => completeStudentLesson(lessonId!, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: studentKeys.summary })
      qc.invalidateQueries({ queryKey: studentKeys.enrollments })
      qc.invalidateQueries({ queryKey: studentKeys.root })
      if (lessonId) {
        qc.invalidateQueries({ queryKey: studentKeys.lessonDetail(lessonId) })
      }
    },
  })
}

/**
 * Marks a lesson InProgress. Safe to call multiple times — the backend is idempotent.
 * Errors are intentionally swallowed; failure to start should never block the student
 * from reading lesson content.
 */
export function useStartStudentLesson() {
  return useMutation({
    mutationFn: (lessonId: string) => startStudentLesson(lessonId),
    // no onSuccess cache invalidation — start only changes InProgress count;
    // the lesson detail query will reflect the updated status on its own refetch.
  })
}