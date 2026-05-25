import apiClient from '@/services/api-client'
import type {
  StartTutorSessionRequest,
  StartTutorSessionResponse,
  TutorAskRequest,
  TutorReplyDto,
} from '@/types/tutor'

const BASE = '/student/tutor/sessions'

/** Opens a new tutor session for the given lesson. */
export const startTutorSession = (
  req: StartTutorSessionRequest,
): Promise<StartTutorSessionResponse> =>
  apiClient.post<StartTutorSessionResponse>(BASE, req).then(r => r.data)

/** Sends a student message and receives the AI tutor reply. */
export const askTutor = (
  conversationId: string,
  req: TutorAskRequest,
): Promise<TutorReplyDto> =>
  apiClient.post<TutorReplyDto>(`${BASE}/${conversationId}/ask`, req).then(r => r.data)

/**
 * Ends the active tutor session.
 * Backend is idempotent — safe to call even if already ended.
 * Callers should swallow errors (best-effort cleanup on unmount).
 */
export const endTutorSession = (conversationId: string): Promise<void> =>
  apiClient.post(`${BASE}/${conversationId}/end`).then(() => undefined)
