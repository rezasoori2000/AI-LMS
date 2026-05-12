/**
 * content.service.ts — Admin content API calls.
 *
 * All functions call the backend admin endpoints:
 *   GET/POST/PUT/DELETE /api/admin/grades
 *   GET/POST/PUT/DELETE /api/admin/subjects
 *   GET/POST/PUT/DELETE /api/admin/chapters  (supports ?subjectId=&gradeId= filters)
 *   GET/POST/PUT/DELETE /api/admin/lessons   (supports ?chapterId= filter)
 *
 * Do not use apiClient directly in pages — import from here.
 */
import apiClient from '@/services/api-client'
import type {
  GradeDto,
  CreateGradePayload,
  UpdateGradePayload,
  SubjectDto,
  CreateSubjectPayload,
  UpdateSubjectPayload,
  ChapterDto,
  CreateChapterPayload,
  UpdateChapterPayload,
  LessonDto,
  CreateLessonPayload,
  UpdateLessonPayload,
  QuestionDto,
  CreateQuestionPayload,
  UpdateQuestionPayload,
} from '@/types/content'

// ── Grades ────────────────────────────────────────────────────────────────────

export const getGrades = (): Promise<GradeDto[]> =>
  apiClient.get<GradeDto[]>('/admin/grades').then(r => r.data)

export const getGrade = (id: string): Promise<GradeDto> =>
  apiClient.get<GradeDto>(`/admin/grades/${id}`).then(r => r.data)

export const createGrade = (payload: CreateGradePayload): Promise<GradeDto> =>
  apiClient.post<GradeDto>('/admin/grades', payload).then(r => r.data)

export const updateGrade = (id: string, payload: UpdateGradePayload): Promise<GradeDto> =>
  apiClient.put<GradeDto>(`/admin/grades/${id}`, payload).then(r => r.data)

export const deleteGrade = (id: string): Promise<void> =>
  apiClient.delete(`/admin/grades/${id}`).then(() => undefined)

// ── Subjects ──────────────────────────────────────────────────────────────────

export const getSubjects = (): Promise<SubjectDto[]> =>
  apiClient.get<SubjectDto[]>('/admin/subjects').then(r => r.data)

export const getSubject = (id: string): Promise<SubjectDto> =>
  apiClient.get<SubjectDto>(`/admin/subjects/${id}`).then(r => r.data)

export const createSubject = (payload: CreateSubjectPayload): Promise<SubjectDto> =>
  apiClient.post<SubjectDto>('/admin/subjects', payload).then(r => r.data)

export const updateSubject = (id: string, payload: UpdateSubjectPayload): Promise<SubjectDto> =>
  apiClient.put<SubjectDto>(`/admin/subjects/${id}`, payload).then(r => r.data)

export const deleteSubject = (id: string): Promise<void> =>
  apiClient.delete(`/admin/subjects/${id}`).then(() => undefined)

// ── Chapters ──────────────────────────────────────────────────────────────────

export interface ChapterListFilters {
  subjectId?: string
  gradeId?: string
}

export const getChapters = (filters?: ChapterListFilters): Promise<ChapterDto[]> => {
  const params: Record<string, string> = {}
  if (filters?.subjectId) params.subjectId = filters.subjectId
  if (filters?.gradeId)   params.gradeId   = filters.gradeId
  return apiClient.get<ChapterDto[]>('/admin/chapters', { params }).then(r => r.data)
}

export const getChapter = (id: string): Promise<ChapterDto> =>
  apiClient.get<ChapterDto>(`/admin/chapters/${id}`).then(r => r.data)

export const createChapter = (payload: CreateChapterPayload): Promise<ChapterDto> =>
  apiClient.post<ChapterDto>('/admin/chapters', payload).then(r => r.data)

export const updateChapter = (id: string, payload: UpdateChapterPayload): Promise<ChapterDto> =>
  apiClient.put<ChapterDto>(`/admin/chapters/${id}`, payload).then(r => r.data)

export const deleteChapter = (id: string): Promise<void> =>
  apiClient.delete(`/admin/chapters/${id}`).then(() => undefined)

// ── Lessons ───────────────────────────────────────────────────────────────────

export const getLessons = (chapterId?: string): Promise<LessonDto[]> => {
  const params: Record<string, string> = {}
  if (chapterId) params.chapterId = chapterId
  return apiClient.get<LessonDto[]>('/admin/lessons', { params }).then(r => r.data)
}

export const getLesson = (id: string): Promise<LessonDto> =>
  apiClient.get<LessonDto>(`/admin/lessons/${id}`).then(r => r.data)

export const createLesson = (payload: CreateLessonPayload): Promise<LessonDto> =>
  apiClient.post<LessonDto>('/admin/lessons', payload).then(r => r.data)

export const updateLesson = (id: string, payload: UpdateLessonPayload): Promise<LessonDto> =>
  apiClient.put<LessonDto>(`/admin/lessons/${id}`, payload).then(r => r.data)

export const deleteLesson = (id: string): Promise<void> =>
  apiClient.delete(`/admin/lessons/${id}`).then(() => undefined)

// ── Questions ─────────────────────────────────────────────────────────────────
//
// GET  /api/admin/questions          — all questions (optional ?lessonId= filter)
// GET  /api/admin/questions/:id
// POST /api/admin/questions
// PUT  /api/admin/questions/:id
// DELETE /api/admin/questions/:id

export const getQuestions = (lessonId?: string): Promise<QuestionDto[]> => {
  const params: Record<string, string> = {}
  if (lessonId) params.lessonId = lessonId
  return apiClient.get<QuestionDto[]>('/admin/questions', { params }).then(r => r.data)
}

export const getQuestion = (id: string): Promise<QuestionDto> =>
  apiClient.get<QuestionDto>(`/admin/questions/${id}`).then(r => r.data)

export const createQuestion = (payload: CreateQuestionPayload): Promise<QuestionDto> =>
  apiClient.post<QuestionDto>('/admin/questions', payload).then(r => r.data)

export const updateQuestion = (id: string, payload: UpdateQuestionPayload): Promise<QuestionDto> =>
  apiClient.put<QuestionDto>(`/admin/questions/${id}`, payload).then(r => r.data)

export const deleteQuestion = (id: string): Promise<void> =>
  apiClient.delete(`/admin/questions/${id}`).then(() => undefined)
