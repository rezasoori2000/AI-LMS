import apiClient from '@/services/api-client'
import type {
  StudentSummaryDto,
  EnrolledSubjectDto,
  SubjectChaptersDto,
  LessonDetailDto,
  CompleteRequest,
  CompleteResponse,
} from '@/types/student'

export const getStudentSummary = (): Promise<StudentSummaryDto> =>
  apiClient.get<StudentSummaryDto>('/student/summary').then(r => r.data)

export const getStudentEnrollments = (): Promise<EnrolledSubjectDto[]> =>
  apiClient.get<EnrolledSubjectDto[]>('/student/enrollments').then(r => r.data)

export const getStudentSubjectChapters = (subjectId: string): Promise<SubjectChaptersDto> =>
  apiClient.get<SubjectChaptersDto>(`/student/subjects/${subjectId}/chapters`).then(r => r.data)

export const getStudentLesson = (lessonId: string): Promise<LessonDetailDto> =>
  apiClient.get<LessonDetailDto>(`/student/lessons/${lessonId}`).then(r => r.data)

export const completeStudentLesson = (
  lessonId: string,
  payload: CompleteRequest,
): Promise<CompleteResponse> =>
  apiClient.post<CompleteResponse>(`/student/lessons/${lessonId}/complete`, payload).then(r => r.data)

export const startStudentLesson = (lessonId: string): Promise<void> =>
  apiClient.post(`/student/lessons/${lessonId}/start`).then(() => undefined)