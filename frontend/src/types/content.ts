// ============================================================
// Content entity types — match backend Application DTOs exactly.
// Used by content.service.ts and all admin content pages.
// ============================================================

// ── Grades ────────────────────────────────────────────────────────────────────

export interface GradeDto {
  id: string
  name: string
  level: number
  tenantId: string | null
  createdAt: string
  updatedAt: string | null
}

/** Controlled form state — level is a string so the <input type="number"> works naturally. */
export interface GradeFormState {
  name: string
  level: string
}

export interface CreateGradePayload {
  name: string
  level: number
}

export interface UpdateGradePayload {
  name: string
  level: number
}

// ── Subjects ──────────────────────────────────────────────────────────────────

export interface SubjectDto {
  id: string
  name: string
  slug: string
  description: string | null
  tenantId: string | null
  createdAt: string
  updatedAt: string | null
}

export interface SubjectFormState {
  name: string
  slug: string
  description: string
}

export interface CreateSubjectPayload {
  name: string
  slug: string
  description?: string
}

export interface UpdateSubjectPayload {
  name: string
  slug: string
  description?: string
}

// ── Chapters ──────────────────────────────────────────────────────────────────

export interface ChapterDto {
  id: string
  subjectId: string
  gradeId: string
  title: string
  description: string | null
  order: number
  tenantId: string | null
  createdAt: string
  updatedAt: string | null
}

export interface ChapterFormState {
  subjectId: string
  gradeId: string
  title: string
  description: string
  order: string
}

export interface CreateChapterPayload {
  subjectId: string
  gradeId: string
  title: string
  order: number
  description?: string
}

export interface UpdateChapterPayload {
  title: string
  order: number
  description?: string
}

// ── Lessons ───────────────────────────────────────────────────────────────────

export interface LessonDto {
  id: string
  chapterId: string
  title: string
  content: string | null
  order: number
  estimatedMinutes: number | null
  tenantId: string | null
  createdAt: string
  updatedAt: string | null
}

export interface LessonFormState {
  chapterId: string
  title: string
  content: string
  order: string
  estimatedMinutes: string
}

export interface CreateLessonPayload {
  chapterId: string
  title: string
  order: number
  content?: string
  estimatedMinutes?: number
}

export interface UpdateLessonPayload {
  title: string
  order: number
  content?: string
  estimatedMinutes?: number
}

// ── Questions ─────────────────────────────────────────────────────────────────

export type QuestionType = 'MultipleChoice' | 'TrueFalse' | 'ShortAnswer'
export type DifficultyLevel = 'Easy' | 'Medium' | 'Hard'

export interface QuestionDto {
  id: string
  lessonId: string | null
  text: string
  type: QuestionType
  difficulty: DifficultyLevel
  optionsJson: string | null
  correctAnswer: string
  tenantId: string | null
  createdAt: string
  updatedAt: string | null
}

/**
 * Controlled form state for QuestionFormPage.
 *
 * Type-specific answer fields are kept separate so the form avoids
 * encoding/decoding the wire format during editing:
 *   MultipleChoice → correctAnswer = correctAnswerIndex,
 *                    optionsJson   = JSON.stringify(options)
 *   TrueFalse      → correctAnswer = correctAnswerBool,
 *                    optionsJson   = null
 *   ShortAnswer    → correctAnswer = correctAnswerText,
 *                    optionsJson   = null
 *
 * All 4 option slots are always present in the tuple; only shown when
 * type = MultipleChoice.
 *
 * Phase 3: replace option slots with a dynamic add/remove list when
 * variable-option counts are required.
 */
export interface QuestionFormState {
  lessonId: string                          // '' = standalone bank question
  text: string
  type: QuestionType | ''
  difficulty: DifficultyLevel | ''
  // MultipleChoice-specific
  options: [string, string, string, string] // always 4 slots
  correctAnswerIndex: string                // '0' | '1' | '2' | '3'
  // TrueFalse-specific
  correctAnswerBool: string                 // 'true' | 'false' | ''
  // ShortAnswer-specific
  correctAnswerText: string
}

export interface CreateQuestionPayload {
  text: string
  type: QuestionType
  difficulty: DifficultyLevel
  correctAnswer: string
  optionsJson?: string
  lessonId?: string
}

export interface UpdateQuestionPayload {
  text: string
  type: QuestionType
  difficulty: DifficultyLevel
  correctAnswer: string
  optionsJson: string | null
  lessonId: string | null
}
