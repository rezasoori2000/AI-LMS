/**
 * Student linkage types — mirrors LMS.Application.Admin.Students DTOs exactly.
 */

/** One student row in the admin linkage list. */
export interface StudentLinkSummaryDto {
  studentId:       string
  fullName:        string
  gradeName:       string | null
  parentProfileId: string | null
  parentFullName:  string | null
  parentEmail:     string | null
}

/** Lightweight parent option for "assign parent" dropdowns. */
export interface ParentOptionDto {
  parentProfileId: string
  fullName:        string
  email:           string
}

/** Request body for PATCH /api/admin/students/{id}/parent. */
export interface AssignParentPayload {
  parentProfileId: string | null
}
