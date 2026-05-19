// ============================================================
// Student portal types — Phase 1 MVP
// Match backend Application/Student/Dtos/StudentDtos.cs exactly.
// Used by student.service.ts and all student portal pages.
// Enums are string-serialised via JsonStringEnumConverter.
// ============================================================

/**
 * Matches LMS.Domain.Progress.ProgressStatus.
 * Serialised as a string by the backend.
 */
export type ProgressStatus = 'NotStarted' | 'InProgress' | 'Completed'

/**
 * Matches LMS.Domain.Catalog.QuestionType.
 * Serialised as a string by the backend.
 */
export type QuestionType = 'MultipleChoice' | 'TrueFalse' | 'ShortAnswer'

// ── Read responses ────────────────────────────────────────────────────────────

/** Dashboard aggregate stats — matches backend StudentSummaryDto. */
export interface StudentSummaryDto {
  activeEnrollments:       number
  completedLessons:        number
  inProgressLessons:       number
  /** (completedLessons / totalLessonsAcrossActiveEnrollments) × 100. 0 when no enrollments. */
  overallProgressPercent:  number
}

/** One enrolled subject in the enrollment list — matches backend EnrolledSubjectDto. */
export interface EnrolledSubjectDto {
  enrollmentId:     string
  subjectId:        string
  subjectName:      string
  subjectSlug:      string
  gradeName:        string | null
  enrolledAt:       string      // ISO 8601
  totalLessons:     number
  completedLessons: number
  /** First NotStarted or InProgress lesson. Null when all lessons are completed. */
  nextLessonId:     string | null
}

/** Full chapter+lesson tree for one subject — matches backend SubjectChaptersDto. */
export interface SubjectChaptersDto {
  subjectId:   string
  subjectName: string
  chapters:    ChapterWithLessonsDto[]
}

export interface ChapterWithLessonsDto {
  chapterId:    string
  chapterTitle: string
  order:        number
  lessons:      LessonSummaryDto[]
}

/** Lightweight lesson row in the subject outline. */
export interface LessonSummaryDto {
  lessonId:         string
  title:            string
  order:            number
  estimatedMinutes: number | null
  progressStatus:   ProgressStatus
}

/**
 * Full lesson content for the lesson viewer.
 * NOTE: questions do NOT include the correct answer.
 */
export interface LessonDetailDto {
  lessonId:         string
  title:            string
  content:          string | null    // Plain text (Phase 1); Phase 3 will introduce Markdown rendering
  estimatedMinutes: number | null
  chapterId:        string
  chapterTitle:     string
  subjectId:        string
  subjectName:      string
  progressStatus:   ProgressStatus
  questions:        QuestionForStudentDto[]
}

/**
 * A question as shown to a student.
 * CorrectAnswer is intentionally absent — never request or display it.
 */
export interface QuestionForStudentDto {
  questionId: string
  text:       string
  type:       QuestionType
  /** Populated for MultipleChoice. Null for TrueFalse and ShortAnswer. */
  options:    string[] | null
}

// ── Progress write payloads ───────────────────────────────────────────────────

/** POST /api/student/lessons/{lessonId}/complete — request body. */
export interface CompleteRequest {
  answers: AnswerDto[]
}

/**
 * One student answer.
 * Encoding:
 *   MultipleChoice → zero-based option index string ("0", "1", "2", "3")
 *   TrueFalse      → "true" or "false"
 *   ShortAnswer    → free text (not evaluated in Phase 1)
 */
export interface AnswerDto {
  questionId: string
  answer:     string
}

/** POST /api/student/lessons/{lessonId}/complete — response. */
export interface CompleteResponse {
  scorePercent:      number | null
  correctCount:      number
  gradableQuestions: number
  results:           AnswerResultDto[]
}

/**
 * Per-question result after lesson completion.
 * Phase 1: isCorrect only. Correct answer not revealed in Phase 1.
 */
export interface AnswerResultDto {
  questionId: string
  isCorrect:  boolean
}
