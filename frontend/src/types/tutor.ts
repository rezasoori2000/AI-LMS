// ============================================================
// AI Tutor types — Phase 1 MVP
// Match backend Application/AiTutor/TutorDtos.cs exactly.
// Used by tutor.service.ts and TutorPanel.
// ============================================================

/** POST /api/student/tutor/sessions — request body. */
export interface StartTutorSessionRequest {
  lessonId: string
}

/** POST /api/student/tutor/sessions — response. */
export interface StartTutorSessionResponse {
  conversationId: string
  lessonId: string
}

/**
 * POST /api/student/tutor/sessions/{conversationId}/ask — request body.
 *
 * message: max 1,000 characters (enforced by backend).
 * hintForQuestionId: the specific lesson question to hint for.
 *   Deferred from the Phase 1 UI — field is present for extensibility.
 */
export interface TutorAskRequest {
  message: string
  hintForQuestionId?: string
}

/** POST /api/student/tutor/sessions/{conversationId}/ask — response. */
export interface TutorReplyDto {
  conversationId: string
  reply:          string
  tokensUsed:     number | null
}
