import { useMutation } from '@tanstack/react-query'
import { startTutorSession, askTutor } from '@/services/tutor.service'
import type { TutorReplyDto } from '@/types/tutor'

interface AskParams {
  lessonId:       string
  conversationId: string | null
  message:        string
}

interface AskResult extends TutorReplyDto {
  /**
   * The session ID used for this exchange.
   * May be newly created if this was the student's first ask in this session.
   */
  resolvedConversationId: string
}

/**
 * Mutation hook for the one-step tutor ask flow.
 *
 * On the first call it lazily starts a tutor session, then sends the
 * question.  Returns the AI reply together with the (possibly new)
 * conversationId so the caller can store it for follow-up asks.
 *
 * Error/loading state is provided by TanStack Query:
 *   isPending  — AI call in flight
 *   isError    — start or ask failed
 *   error      — the thrown Error
 *   data       — last successful AskResult
 */
export function useTutorAsk() {
  return useMutation<AskResult, Error, AskParams>({
    mutationFn: async ({ lessonId, conversationId, message }) => {
      let convId = conversationId

      // Lazily open the session on the first question.
      if (!convId) {
        const session = await startTutorSession({ lessonId })
        convId = session.conversationId
      }

      const reply = await askTutor(convId, { message })
      return { ...reply, resolvedConversationId: convId }
    },
  })
}
