# AI Tutor MVP

Phase 1, Section 11 defines and implements the AI tutor MVP foundation.
No live LLM calls are made in Phase 1 — the full orchestration chain is wired
and tested with a deterministic stub provider. The AI service guardrail layer
is production-ready and waiting for a real provider to be configured.

---

## What the tutor does

- Helps a student understand the **current lesson** — not any other topic
- Explains concepts in clear, grade-appropriate language
- Re-states ideas in simpler terms when asked
- Offers hints and guiding questions — never direct answers
- Points to the relevant part of the lesson content when useful

## What the tutor does NOT do

- Answer questions outside the current lesson
- Give away correct answers, even if asked directly
- Modify any student progress, scores, or academic records
- Communicate results to teachers or parents
- Act as a general-purpose assistant or tutor for other subjects
- Grade student work

---

## Architecture

```
Student browser
    │  POST /api/student/tutor/sessions               (start session)
    │  POST /api/student/tutor/sessions/{id}/ask      (send message)
    │  POST /api/student/tutor/sessions/{id}/end      (end session)
    ▼
Backend API  [Authorize(Roles = "Student")]
    │  TutorController → ITutorService
    │  ├─ JWT required; role = "Student"
    │  ├─ Enrollment check → 403 if not enrolled in lesson's subject
    │  ├─ Conversation ownership check → 403 if conversation does not belong to student
    │  ├─ Assemble TutorContextSnapshot (CorrectAnswer excluded at query level)
    │  └─ ITutorProvider.GetReplyAsync()
    ▼
AI Service  (internal — not internet-facing, no public auth)
    │  POST /api/v1/tutor/ask
    │  Builds guardrail-aware system prompt + conversation prompt
    │  Calls LLM provider
    ▼
LLM Provider
    Phase 1: StubTutorProvider (deterministic placeholder, no LLM call)
    Phase 2+: Ollama / OpenAI / Anthropic (configured via ai-service settings)
```

### Key files

| Layer | File | Purpose |
|-------|------|---------|
| Backend — service | `LMS.Application/AiTutor/TutorService.cs` | Orchestration: enrollment → assembly → provider → persist |
| Backend — context | `LMS.Application/AiTutor/TutorContextAssembler.cs` | DB → `TutorContextSnapshot`; enforces CorrectAnswer exclusion |
| Backend — contract | `LMS.Application/AiTutor/TutorContextSnapshot.cs` | Context type encoding security invariants |
| Backend — boundaries | `LMS.Application/AiTutor/TutorBoundaryNotes.cs` | Architecture reference (code comments) |
| Backend — provider stub | `LMS.Infrastructure/AI/StubTutorProvider.cs` | Phase 1 placeholder; swap in Phase 2 |
| AI service — prompts | `ai-service/app/core/tutor_prompts.py` | System prompt builder + guardrail constants |
| AI service — endpoint | `ai-service/app/api/v1/tutor.py` | `POST /api/v1/tutor/ask` |
| AI service — models | `ai-service/app/models/tutor.py` | Pydantic contract (service-to-service) |
| Frontend — UI | `frontend/src/features/student/components/TutorPanel.tsx` | Inline lesson-help panel |
| Frontend — hook | `frontend/src/features/student/hooks/useTutor.ts` | Lazy session start + ask mutation |
| Frontend — service | `frontend/src/services/tutor.service.ts` | API client calls |

---

## Context sent to the AI service

Every `/ask` request sends a `TutorContextSnapshot` assembled by `TutorContextAssembler`.
Context assembly only runs if the student is enrolled and the conversation is active.

### Always included

| Field | Value |
|-------|-------|
| `student_first_name` | First name only — no last name in AI payloads |
| `lesson_title` | e.g. "Introduction to Fractions" |
| `lesson_content` | Full lesson text (plain text / Markdown) |
| `grade_name` | e.g. "Grade 4" |
| `subject_name` | e.g. "Mathematics" |
| `lesson_progress_status` | "NotStarted" \| "InProgress" \| "Completed" |
| `conversation_id` | Current session ID |
| `history` | All messages sent; AI service prompt builder uses last 10 turns |

### Added only for hint requests (`hint_for_question_id ≠ null`)

| Field | Value |
|-------|-------|
| `question_text` | The question text |
| `question_type` | "MultipleChoice" \| "TrueFalse" \| "ShortAnswer" |
| `question_options` | Option texts for MultipleChoice (4 items) |

### Never included

- `CorrectAnswer` — excluded at the EF Core query level in `TutorContextAssembler`
- Other students' data
- `LearnerProfile`, `TopicMasterySnapshot` (Phase 3)
- Cross-session conversation history
- Teacher notes, parent notes

---

## Guardrail design

All behavioral constraints live in `ai-service/app/core/tutor_prompts.py`.
Guardrails are explicit system prompt instructions — no post-processing filter.

| Rule | Behavior |
|------|----------|
| LESSON SCOPE | Answer only from the provided lesson content; use `OUT_OF_SCOPE_REDIRECT` phrase if not covered |
| NO DIRECT ANSWERS | Never reveal the correct answer; use `ANSWER_WITHHOLD_REDIRECT` phrase if pressed |
| HONEST LIMITS | Acknowledge gaps rather than fabricating |
| APPROPRIATE & CONCISE | 2–3 paragraphs; grade-level language; no lecture-length responses |
| OFF-TOPIC / INAPPROPRIATE | Fixed `OFF_TOPIC_REDIRECT` phrase; no engagement |

**Key constants:**

| Constant | Value |
|----------|-------|
| `TUTOR_TEMPERATURE` | `0.3` (low — reduces hallucination for factual content) |
| `TUTOR_MAX_RESPONSE_TOKENS` | `600` (2–4 paragraphs) |
| `HISTORY_WINDOW_TURNS` | `10` (oldest turns dropped first) |

**Recency bias**: Guardrail rules are placed at the END of the system prompt so recent LLMs
weight them more heavily than earlier context.

---

## Data ownership

`ITutorService` **may write**:
- `AiConversation` — create (start), end (end)
- `AiMessage` — append via `AiConversation.AddMessage()`

`ITutorService` **may read** (no writes):
- `Lesson` — content, title, grade, subject
- `LessonProgress` — status only
- `Question` — text, type, options; `CorrectAnswer` never selected
- `Enrollment` — active status check only
- `StudentProfile` — id, first name, grade

`ITutorService` **must never touch**:
- `QuestionAnswerRecord` — owned by `IStudentService.CompleteLessonAsync`
- `LessonProgress` state transitions — owned by `IStudentService`
- Any admin, teacher, or parent tables

---

## Phase 1 state

| Component | State |
|-----------|-------|
| Backend orchestration (`TutorService`) | ✅ Fully implemented |
| Context assembly (`TutorContextAssembler`) | ✅ Fully implemented; CorrectAnswer excluded |
| LLM provider | `StubTutorProvider` — deterministic placeholder; no LLM calls |
| AI service endpoint | ✅ Fully implemented with guardrail-aware prompts |
| AI service guardrails | ✅ Complete; returns 503 if no provider configured |
| Frontend tutor panel | ✅ Inline `SectionCard` in `LessonPlayerPage` |
| Session lifecycle | ✅ Start (lazy) → Ask → End (on unmount) |

---

## Readiness checklist for Phase 2 AI work

- [ ] Wire `HttpTutorProvider` in backend DI (replace `StubTutorProvider`)
- [ ] Configure AI service provider in `ai-service/app/core/config.py` (Ollama or cloud)
- [ ] Add backend integration test for full start → ask → end cycle with real provider
- [ ] Cap `History` before serializing in `TutorContextAssembler` (all messages currently sent; AI service caps at 10 for prompt — wasteful for long sessions)
- [ ] Expose `hintForQuestionId` in `TutorPanel` UI (field exists in types and service; deferred in Part 3)
- [ ] Validate guardrail effectiveness with real LLM responses
- [ ] Add token usage reporting (currently `tokens_used: null` in Phase 1 stub)

---

## Intentionally deferred

| Feature | Deferred to |
|---------|-------------|
| Real LLM provider integration | Phase 2 |
| `hintForQuestionId` UI in `TutorPanel` | Phase 2 |
| Conversation history display in UI | Phase 2 |
| Backend `History` cap before serialization | Phase 2 |
| Token usage tracking and reporting | Phase 2 |
| Cross-session learner memory | Phase 3 |
| `LearnerProfile` / `TopicMasterySnapshot` context enrichment | Phase 3 |
| RAG / vector search pipeline | Phase 3 |
| Streaming responses | Phase 3 nice-to-have |
| ShortAnswer AI grading | Phase 3 |
| Teacher / parent AI summaries | Phase 3+ |
| Recommendation engine | Phase 3+ |
| Autonomous academic record mutation | Never |
| Open-domain chat beyond the lesson | Never |
| Emotional support / counseling behavior | Never |
