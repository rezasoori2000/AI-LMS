/**
 * teacher.service.ts — Teacher portal read API calls.
 *
 * All functions call the backend teacher endpoints:
 *   GET /api/teacher/summary
 *   GET /api/teacher/students
 *   GET /api/teacher/students/{studentId}
 *   GET /api/teacher/students/{studentId}/progress
 *
 * Ownership scoping is enforced server-side — these endpoints only return
 * data for students assigned to the calling teacher via the
 * TeacherStudentAssignment join table.
 * Accessing an unassigned student returns HTTP 403.
 *
 * Do not use apiClient directly in pages — import from here.
 */
import apiClient from '@/services/api-client'
import type {
  TeacherSummaryDto,
  AssignedStudentSummaryDto,
  TeacherStudentDetailDto,
  TeacherStudentProgressDto,
} from '@/types/teacher'

/** Returns dashboard stat counts for the calling teacher. */
export const getTeacherSummary = (): Promise<TeacherSummaryDto> =>
  apiClient.get<TeacherSummaryDto>('/teacher/summary').then(r => r.data)

/** Returns all students assigned to the calling teacher. */
export const getMyStudents = (): Promise<AssignedStudentSummaryDto[]> =>
  apiClient.get<AssignedStudentSummaryDto[]>('/teacher/students').then(r => r.data)

/**
 * Returns enrollment detail for one assigned student.
 * Rejects (HTTP 403) when the student is not assigned to the calling teacher.
 */
export const getStudentDetail = (studentId: string): Promise<TeacherStudentDetailDto> =>
  apiClient.get<TeacherStudentDetailDto>(`/teacher/students/${studentId}`).then(r => r.data)

/**
 * Returns lesson-level progress for one assigned student.
 * Rejects (HTTP 403) when the student is not assigned to the calling teacher.
 */
export const getStudentProgress = (studentId: string): Promise<TeacherStudentProgressDto> =>
  apiClient.get<TeacherStudentProgressDto>(`/teacher/students/${studentId}/progress`).then(r => r.data)
