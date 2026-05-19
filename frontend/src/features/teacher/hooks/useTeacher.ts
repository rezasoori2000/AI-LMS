import { useQuery } from '@tanstack/react-query'
import {
  getTeacherSummary,
  getMyStudents,
  getStudentDetail,
  getStudentProgress,
} from '@/services/teacher.service'

// Stable query key factory — centralised so invalidations always match.
export const teacherKeys = {
  summary:         ['teacher', 'summary']                                        as const,
  students:        ['teacher', 'students']                                       as const,
  studentDetail:   (studentId: string) => ['teacher', 'students', studentId]    as const,
  studentProgress: (studentId: string) => ['teacher', 'students', studentId, 'progress'] as const,
}

/** Fetches dashboard summary stats for the calling teacher. */
export function useTeacherSummary() {
  return useQuery({
    queryKey: teacherKeys.summary,
    queryFn:  getTeacherSummary,
  })
}

/**
 * Fetches all students assigned to the calling teacher.
 * Returns an empty array when no students are assigned — never throws on 200.
 */
export function useMyStudents() {
  return useQuery({
    queryKey: teacherKeys.students,
    queryFn:  getMyStudents,
  })
}

/**
 * Fetches enrollment detail for one assigned student.
 * Disabled when studentId is undefined or empty.
 * Rejects with HTTP 403 when the student is not assigned to the calling teacher.
 */
export function useStudentDetail(studentId: string | undefined) {
  return useQuery({
    queryKey: teacherKeys.studentDetail(studentId ?? ''),
    queryFn:  () => getStudentDetail(studentId!),
    enabled:  Boolean(studentId),
  })
}

/**
 * Fetches lesson-level progress for one assigned student.
 * Disabled when studentId is undefined or empty.
 * Rejects with HTTP 403 when the student is not assigned to the calling teacher.
 */
export function useStudentProgress(studentId: string | undefined) {
  return useQuery({
    queryKey: teacherKeys.studentProgress(studentId ?? ''),
    queryFn:  () => getStudentProgress(studentId!),
    enabled:  Boolean(studentId),
  })
}
