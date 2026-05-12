/**
 * parent.service.ts — Parent portal read API calls.
 *
 * All functions call the backend parent endpoints:
 *   GET /api/parent/children
 *   GET /api/parent/children/{studentId}
 *
 * Ownership scoping is enforced server-side — these endpoints only return
 * data for students linked to the calling parent's ParentProfile.
 *
 * Do not use apiClient directly in pages — import from here.
 */
import apiClient from '@/services/api-client'
import type { ChildSummaryDto, ChildDetailResponse } from '@/types/parent'

/**
 * Returns all students linked to the calling parent.
 * Returns an empty array when no students are linked — never throws.
 */
export const getMyChildren = (): Promise<ChildSummaryDto[]> =>
  apiClient.get<ChildSummaryDto[]>('/parent/children').then(r => r.data)

/**
 * Returns the full detail for one linked child.
 * Rejects (HTTP 403) when the student is not linked to the calling parent.
 */
export const getChildDetail = (studentId: string): Promise<ChildDetailResponse> =>
  apiClient.get<ChildDetailResponse>(`/parent/children/${studentId}`).then(r => r.data)
