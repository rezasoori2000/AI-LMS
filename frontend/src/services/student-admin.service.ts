/**
 * student-admin.service.ts — Admin student linkage API calls.
 *
 * Endpoints:
 *   GET  /api/admin/students              → list all students with linkage state
 *   GET  /api/admin/students/parent-options → parent dropdown options
 *   PATCH /api/admin/students/{id}/parent  → assign or unlink parent
 */
import apiClient from '@/services/api-client'
import type {
  StudentLinkSummaryDto,
  ParentOptionDto,
  AssignParentPayload,
} from '@/types/student-admin'

export async function getStudents(): Promise<StudentLinkSummaryDto[]> {
  const res = await apiClient.get<StudentLinkSummaryDto[]>('/api/admin/students')
  return res.data
}

export async function getParentOptions(): Promise<ParentOptionDto[]> {
  const res = await apiClient.get<ParentOptionDto[]>('/api/admin/students/parent-options')
  return res.data
}

export async function assignParent(
  studentId: string,
  payload: AssignParentPayload,
): Promise<StudentLinkSummaryDto> {
  const res = await apiClient.patch<StudentLinkSummaryDto>(
    `/api/admin/students/${studentId}/parent`,
    payload,
  )
  return res.data
}
