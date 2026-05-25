import { useEffect, useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { InlineFeedback } from '@/components/feedback'
import { Button, SectionCard, Textarea } from '@/components/ui'
import { useTutorAsk } from '@/features/student/hooks/useTutor'
import { endTutorSession } from '@/services/tutor.service'

// ── Props ─────────────────────────────────────────────────────────────────────

interface TutorPanelProps {
  /** The lesson the student is currently viewing. Passed to the backend. */
  lessonId:    string
  /** Shown in the card subtitle so the tutor's scope is obvious. */
  lessonTitle: string
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * TutorPanel — inline AI lesson-help section.
 *
 * Renders as a SectionCard at the bottom of the lesson view.
 * Manages one active question/response cycle at a time.
 *
 * Session lifecycle
 *   - The tutor session is started lazily on the student's first ask.
 *   - conversationId is stored in local state; subsequent asks reuse it.
 *   - The session is ended silently when the panel unmounts.
 *
 * IMPORTANT: mount this with `key={lessonId}` in the parent so it remounts
 * when the student navigates to a different lesson.  This:
 *   (a) triggers the cleanup effect to end the old session, and
 *   (b) resets all local state for the new lesson automatically.
 *
 * Deferred (Phase 2+):
 *   - Follow-up question threading / conversation history display
 *   - "Hint for question" targeting (TutorAskRequest.hintForQuestionId)
 *   - Suggested question chips
 *   - Teacher/parent visibility into tutor sessions
 */
export function TutorPanel({ lessonId, lessonTitle }: TutorPanelProps) {
  const [question, setQuestion]           = useState('')
  const [conversationId, setConversationId] = useState<string | null>(null)
  const [lastReply, setLastReply]         = useState<string | null>(null)

  // Ref mirrors conversationId so the cleanup effect always reads the latest
  // value without including it in the dependency array.
  const conversationIdRef = useRef<string | null>(null)
  useEffect(() => {
    conversationIdRef.current = conversationId
  }, [conversationId])

  // Best-effort session end on unmount (lesson change or page leave).
  useEffect(() => {
    return () => {
      const convId = conversationIdRef.current
      if (convId) {
        endTutorSession(convId).catch(() => {
          // Intentionally swallowed — end is non-critical; the backend marks
          // sessions ended when the student re-opens the lesson anyway.
        })
      }
    }
  }, [])

  const tutorAsk = useTutorAsk()
  const hasReply = lastReply !== null

  function handleAsk() {
    if (!question.trim() || tutorAsk.isPending) return

    tutorAsk.mutate(
      { lessonId, conversationId, message: question.trim() },
      {
        onSuccess: result => {
          setConversationId(result.resolvedConversationId)
          setLastReply(result.reply)
          setQuestion('')
        },
      },
    )
  }

  function handleKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    // Ctrl/Cmd + Enter to submit — mirrors common chat shortcut patterns.
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault()
      handleAsk()
    }
  }

  return (
    <SectionCard
      title="AI Lesson Help"
      description={`Lesson-grounded help for: ${lessonTitle}`}
    >
      <div className="space-y-4">

        {/* ── Last tutor reply ─────────────────────────────────────────────── */}
        {hasReply && (
          <div
            className="rounded-md border border-info bg-info-light p-4"
            aria-live="polite"
            aria-label="AI tutor response"
          >
            <p className="mb-1.5 text-xs font-semibold uppercase tracking-wide text-info-dark">
              AI Tutor
            </p>
            <p className="text-sm leading-6 text-content-primary whitespace-pre-wrap">
              {lastReply}
            </p>
          </div>
        )}

        {/* ── Empty state (before first ask) ──────────────────────────────── */}
        {!hasReply && !tutorAsk.isPending && (
          <p className="text-sm text-content-secondary">
            Need help with something in this lesson? Ask a question and the AI
            tutor will explain or hint — grounded in the current lesson content.
          </p>
        )}

        {/* ── Question input ───────────────────────────────────────────────── */}
        <Textarea
          label="Your question"
          rows={3}
          value={question}
          onChange={e => setQuestion(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="e.g. Can you re-explain the main concept? What does this term mean?"
          disabled={tutorAsk.isPending}
          maxLength={1000}
        />

        {/* ── Error feedback ───────────────────────────────────────────────── */}
        {tutorAsk.isError && (
          <InlineFeedback
            intent="error"
            message="Could not get a tutor response. Please try again."
          />
        )}

        {/* ── Submit row ───────────────────────────────────────────────────── */}
        <div className="flex flex-wrap items-center gap-4">
          <Button
            size="sm"
            onClick={handleAsk}
            isLoading={tutorAsk.isPending}
            disabled={!question.trim() || tutorAsk.isPending}
          >
            {tutorAsk.isPending
              ? 'Getting help…'
              : hasReply
                ? 'Ask another question'
                : 'Ask AI tutor'}
          </Button>
          <p className="text-xs text-content-muted">
            Ctrl+Enter to submit · AI-generated, lesson-grounded
          </p>
        </div>

      </div>
    </SectionCard>
  )
}
