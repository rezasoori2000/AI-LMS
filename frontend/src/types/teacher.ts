// ============================================================
// Teacher portal types — Phase 1 MVP
// Match backend Application/Teacher/Dtos/TeacherDtos.cs exactly.
// Used by teacher.service.ts and all teacher portal pages.
// Enums are string-serialised via JsonStringEnumConverter.
// ============================================================

/**
 * Matches LMS.Domain.Enrollments.EnrollmentStatus.
 * Serialised as a string by the backend.
 */
export type EnrollmentStatus = 'Active' | 'Completed' | 'Dropped'

/**
 * Matches LMS.Domain.Progress.ProgressStatus.
 * Serialised as a string by the backend.
 */
export type ProgressStatus = 'NotStarted' | 'InProgress' | 'Completed'

// ── Read responses ────────────────────────────────────────────────────────────

/** Dashboard summary stat row — matches backend TeacherSummaryDto. */
export interface TeacherSummaryDto {
  totalAssignedStudents:  number
  activeEnrollments:      number
  lessonsCompletedThisWeek: number
}

/** One row in the assigned-students list — matches backend AssignedStudentSummaryDto. */
export interface AssignedStudentSummaryDto {
  studentId:             string
  fullName:              string
  email:                 string
  gradeName:             string | null
  activeEnrollmentCount: number
  totalLessonsCompleted: number
}

/**
 * Full student detail with enrollment list.
 * Matches backend TeacherStudentDetailDto.
 */
export interface TeacherStudentDetailDto {
  studentId:   string
  fullName:    string
  email:       string
  gradeName:   string | null
  enrollments: TeacherEnrollmentItemDto[]
}

/** Per-enrollment summary within the teacher student detail view. */
export interface TeacherEnrollmentItemDto {
  enrollmentId:     string
  subjectId:        string
  subjectName:      string
  status:           EnrollmentStatus
  enrolledAt:       string       // ISO 8601
  lessonsTotal:     number
  lessonsCompleted: number
  lessonsInProgress: number
  lastActivityAt:   string | null
}

/**
 * Lesson-level progress for one student across active enrollments.
 * Matches backend TeacherStudentProgressDto.
 */
export interface TeacherStudentProgressDto {
  studentId:      string
  fullName:       string
  lessonProgress: TeacherLessonProgressItemDto[]
}

/** One lesson row in the per-student progress view. */
export interface TeacherLessonProgressItemDto {
  lessonId:     string
  lessonTitle:  string
  subjectId:    string
  subjectName:  string
  chapterTitle: string
  status:       ProgressStatus
  scorePercent: number | null
  startedAt:    string | null   // ISO 8601
  completedAt:  string | null   // ISO 8601
}
