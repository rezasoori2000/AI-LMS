namespace LMS.Application.AiTutor;

// ── PHASE 1 SECTION 11 — AI TUTOR ARCHITECTURE NOTES ────────────────────────
//
// This file documents the architecture decisions and hard boundaries for the AI
// tutor module. There is no business logic here — documentation only.
//
// ── Call architecture ─────────────────────────────────────────────────────────
//
//  Student browser
//      │  POST /api/student/tutor/sessions               (start session)
//      │  POST /api/student/tutor/sessions/{id}/ask      (send message)
//      │  POST /api/student/tutor/sessions/{id}/end      (end session)
//      ▼
//  Backend API  [Authorize(Roles = "Student")]
//      │  TutorController  →  ITutorService
//      │  ├─ Auth: valid Student JWT required
//      │  ├─ Ownership: enrollment check (StudentAccessDeniedException → 403)
//      │  ├─ Assemble TutorContextSnapshot (see context boundary below)
//      │  └─ HTTP POST to AI service (internal, never public internet)
//      ▼
//  AI Service  (internal only — no public auth, not internet-facing)
//      │  POST /api/v1/tutor/ask
//      │  Input: TutorAskPayload { context_snapshot, student_message }
//      ▼
//  LLM Provider  (Ollama / OpenAI / Anthropic — configured via Settings)
//      │  BaseLLMProvider.complete(prompt, system_prompt, model, max_tokens)
//      ▼
//  AI Service returns TutorAskResponse { reply, tokens_used }
//      │
//  Backend persists AiMessage records via AiConversation.AddMessage()
//      │
//  Backend returns TutorReplyDto to the frontend
//
// ── MVP feature scope ─────────────────────────────────────────────────────────
//
//  ✅ INCLUDED in Phase 1 Section 11 (Parts 2+):
//     - Open a tutor session for the lesson the student is currently viewing
//     - Ask free-text curriculum questions about the lesson content
//     - Request a hint for a specific question (without revealing the answer)
//     - Receive a simplified re-explanation of lesson material
//     - Conversation history persisted per session (AiConversation / AiMessage)
//     - Session start / end lifecycle (AiConversation.EndedAt)
//
//  ❌ DEFERRED — not in this MVP:
//     - LearnerProfile / LearnerPreferences context enrichment (Phase 3)
//     - TopicMasterySnapshot-aware prompting (Phase 3)
//     - Streaming responses (Phase 3 nice-to-have)
//     - Cross-session learner memory (Phase 3)
//     - Vector-search / RAG pipeline (Phase 3)
//     - ShortAnswer AI grading (Phase 3)
//     - Autonomous lesson grading or academic record mutation (never — owned by StudentService)
//     - Parent or teacher notifications triggered from tutor (never)
//     - Emotional support / counseling behavior (out of scope permanently)
//     - Open-domain chat not grounded in the current lesson (out of scope permanently)
//     - AI-generated questions (out of scope for MVP)
//
// ── Context assembly boundary ─────────────────────────────────────────────────
//
//  ALWAYS included in TutorContextSnapshot:
//    - Student first name (tone personalization — no last name in AI payloads)
//    - Lesson title, content (full plain text), grade name, subject name
//    - Lesson progress status ("NotStarted" | "InProgress" | "Completed")
//    - Conversation history for the current session (all messages sent; AI service
//      prompt builder caps usage at HISTORY_WINDOW_TURNS = 10 turns)
//
//  ADDED ONLY for hint requests (HintForQuestionId ≠ null):
//    - Question text
//    - Question type ("MultipleChoice" | "TrueFalse" | "ShortAnswer")
//    - Question options (MultipleChoice: 4 items)
//    *** CorrectAnswer is NEVER included — not even for hint requests ***
//
//  NEVER included in any TutorContextSnapshot:
//    - CorrectAnswer
//    - Other students' data
//    - LearnerProfile, TopicMasterySnapshot (Phase 3)
//    - AiMessage records from other conversations
//    - Parent or teacher notes
//    - GDPR-sensitive fields beyond first name + grade/subject label
//
// ── Data ownership rules ──────────────────────────────────────────────────────
//
//  ITutorService MAY WRITE:
//    AiConversation  — create (StartSessionAsync), end (EndSessionAsync)
//    AiMessage       — append via AiConversation.AddMessage() (AskAsync)
//
//  ITutorService MAY READ (for context assembly only — no writes):
//    Lesson             — content, title, grade, subject
//    LessonProgress     — status only
//    Question           — text, type, options (never CorrectAnswer)
//    Enrollment         — active status check only
//    StudentProfile     — id, first name, grade
//
//  ITutorService MUST NEVER TOUCH:
//    QuestionAnswerRecord   — owned by IStudentService.CompleteLessonAsync
//    LessonProgress changes — owned by IStudentService
//    User                   — read UserId via ICurrentUserService only
//    Any admin or teacher tables
//
// ── AI service internal endpoint contract ─────────────────────────────────────
//
//  POST /api/v1/tutor/ask
//
//  Request body (ai-service Pydantic: TutorAskPayload):
//    context_snapshot   : TutorContextSnapshot   — assembled by backend
//    student_message    : str                     — max 1,000 chars
//
//  Response body (ai-service Pydantic: TutorAskResponse):
//    reply              : str
//    tokens_used        : int | None
//
//  The AI service endpoint:
//    - Is NOT internet-facing (internal service-to-service only)
//    - Does NOT perform auth (backend already authenticated the student)
//    - DOES validate input via Pydantic before processing
//    - Returns HTTP 503 when the LLM provider is unavailable or not configured
//    - Returns HTTP 422 for malformed request bodies (Pydantic validation)
//
// ── Safety constraints ────────────────────────────────────────────────────────
//
//  1. System prompt MUST instruct the model to stay within lesson content:
//       "You are a helpful tutor for [LessonTitle] only. Answer only from the
//       lesson content provided below. If asked something outside the lesson,
//       say 'That is outside our lesson for today' and redirect the student."
//
//  2. Student message length: capped at 1,000 characters (validated in the
//     backend before forwarding; Pydantic also validates in the AI service).
//
//  3. AI response length: capped at 600 tokens (TUTOR_MAX_RESPONSE_TOKENS in
//     ai-service/app/core/tutor_prompts.py). Enough for 2–4 paragraphs.
//
//  4. CorrectAnswer must NEVER reach the AI service. Context assembly
//     (assembler) is the primary defense; prompt engineering is the second.
//
//  5. No behavioral telemetry on domain entities (time-per-interaction,
//     frustration signals, etc.). See DesignNotes.cs in
//     LMS.Domain/Personalization/ for the permanent exclusion list.
//
// ─────────────────────────────────────────────────────────────────────────────
