/**
 * student-admin.service.ts — Admin student linkage API calls.
 *
 * Endpoints:
 *   GET  /admin/students                      → list all students with linkage state
 *   GET  /admin/students/parent-options       → parent dropdown options
 *   PATCH /admin/students/{id}/parent         → assign or unlink parent
 *   GET  /admin/students/teacher-options      → teacher dropdown options
 *   PATCH /admin/students/{id}/teacher        → assign or unassign teacher
 */
import apiClient from '@/services/api-client'
import type {
  StudentLinkSummaryDto,
  ParentOptionDto,
  AssignParentPayload,
  TeacherOptionDto,
  AssignTeacherPayload,
} from '@/types/student-admin'

export async function getStudents(): Promise<StudentLinkSummaryDto[]> {
  const res = await apiClient.get<StudentLinkSummaryDto[]>('/admin/students')
  return res.data
}

export async function getParentOptions(): Promise<ParentOptionDto[]> {
  const res = await apiClient.get<ParentOptionDto[]>('/admin/students/parent-options')
  return res.data
}

export async function assignParent(
  studentId: string,
  payload: AssignParentPayload,
): Promise<StudentLinkSummaryDto> {
  const res = await apiClient.patch<StudentLinkSummaryDto>(
    `/admin/students/${studentId}/parent`,
    payload,
  )
  return res.data
}

export async function getTeacherOptions(): Promise<TeacherOptionDto[]> {
  const res = await apiClient.get<TeacherOptionDto[]>('/admin/students/teacher-options')
  return res.data
}

export async function assignTeacher(
  studentId: string,
  payload: AssignTeacherPayload,
): Promise<StudentLinkSummaryDto> {
  const res = await apiClient.patch<StudentLinkSummaryDto>(
    `/admin/students/${studentId}/teacher`,
    payload,
  )
  return res.data
}
