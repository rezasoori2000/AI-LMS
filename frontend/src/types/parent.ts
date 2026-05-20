// ============================================================
// Parent portal types — match backend Application/Parent/Dtos exactly.
// Used by parent.service.ts and all parent portal pages.
// Enums are string-serialised via JsonStringEnumConverter.
// ============================================================

/**
 * Matches LMS.Domain.Enrollments.EnrollmentStatus.
 * Serialised as a string by the backend (JsonStringEnumConverter).
 */
export type EnrollmentStatus = 'Active' | 'Completed' | 'Dropped'

/** Lightweight summary for one linked child — matches backend ChildSummaryDto. */
export interface ChildSummaryDto {
  studentId:         string
  fullName:          string
  gradeName:         string | null
  activeEnrollments: number
  lessonsCompleted:  number
  lastActivityAt:    string | null
}

/** Per-enrollment progress summary — matches backend EnrollmentSummaryDto. */
export interface EnrollmentSummaryDto {
  enrollmentId:      string
  subjectId:         string
  subjectName:       string
  status:            EnrollmentStatus
  enrolledAt:        string
  totalLessons:      number
  completedLessons:  number
  inProgressLessons: number
  averageScore:      number | null
}

/** Full response for GET /api/parent/children/{studentId} — matches backend ChildDetailDto. */
export interface ChildDetailDto {
  summary:     ChildSummaryDto
  enrollments: EnrollmentSummaryDto[]
}
