# Phase 1 — Section 11 Readiness: AI Tutor MVP Closeout

## Summary

Section 11 is complete through Part 5. The AI tutor MVP foundation is coherent,
testable, and scoped narrowly around lesson-grounded student help.

Current posture:

- MVP boundaries are explicit in code and docs
- Backend orchestration is implemented and ownership-safe
- AI service guardrails are implemented and tested
- Student-facing tutor UI flow is implemented (narrow one-reply loop)
- High-risk scope expansion remains explicitly deferred

Status: **Section 11 complete; ready to move to the next section**

---

## What Was Validated

### 1. Tutor MVP boundaries

Validated in:

- `backend/src/LMS.Application/AiTutor/TutorBoundaryNotes.cs`
- `docs/ai-tutor-mvp.md`

Confirmed:

- Tutor role is limited to the current lesson
- Direct-answer refusal is a hard rule
- Open-domain chat is out of scope
- Tutor does not own grading, progress state changes, or intervention workflows

### 2. Backend orchestration skeleton

Validated in:

- `backend/src/LMS.Application/AiTutor/TutorService.cs`
- `backend/src/LMS.Api/Controllers/Student/TutorController.cs`

Confirmed:

- Session lifecycle is complete: start, ask, end
- Enrollment and ownership checks are enforced before tutoring actions
- Conversation-ended state returns 409 (`TutorConversationEndedException`)
- Data writes are limited to `AiConversation` and `AiMessage`

### 3. Lesson-grounded context assembly

Validated in:

- `backend/src/LMS.Application/AiTutor/TutorContextAssembler.cs`
- `backend/src/LMS.Application/AiTutor/TutorContextSnapshot.cs`

Confirmed:

- Context includes only lesson/student/session fields needed by the tutor
- `CorrectAnswer` is excluded at query level and absent from DTO contract
- Hint context includes question text/type/options only

### 4. Student-facing lesson-help UI

Validated in:

- `frontend/src/features/student/components/TutorPanel.tsx`
- `frontend/src/features/student/hooks/useTutor.ts`

Confirmed:

- Inline panel is lesson-scoped and low-friction
- Session starts lazily on first ask
- Session end is best-effort on unmount
- UI remains intentionally narrow (single latest reply display)

### 5. Response safety and guardrails

Validated in:

- `ai-service/app/core/tutor_prompts.py`
- `ai-service/app/api/v1/tutor.py`
- `ai-service/tests/test_tutor_prompts.py`

Confirmed:

- Guardrails are explicit system prompt rules (scope, refusal, limits, off-topic)
- Redirect phrases are centralized constants and test-anchored
- Prompt history is capped at 10 turns
- AI service returns 503 for unconfigured/unavailable provider; 422 for invalid payload

---

## Small Cleanup Items Found

### Fixed now in Part 5

- Updated stale values in `TutorBoundaryNotes.cs`:
  - token cap corrected to 600
  - history usage note aligned with 10-turn AI prompt cap
  - endpoint behavior aligned to 503/422 (removed old 501 wording)
- Updated stale comment in `TutorContextSnapshot.cs` to reflect current history behavior
- Added consolidated architecture doc: `docs/ai-tutor-mvp.md`
- Updated README Section 11 summary to reflect Parts 1-5 completion

### Deferred intentionally

- Backend history cap before serialization (currently all messages are sent, AI service caps at prompt-build time)
- TutorPanel support for `hintForQuestionId` input path
- Real provider integration and token usage reporting

---

## Readiness Checklist (Next AI-Focused Sections)

Before expanding tutor capabilities, verify:

- [ ] Replace `StubTutorProvider` with real provider wiring (`HttpTutorProvider` path)
- [ ] Configure provider settings and secrets in ai-service config
- [ ] Add backend integration coverage for full start → ask → end with real provider boundary
- [ ] Cap history in backend before payload serialization to reduce long-session payload size
- [ ] Add explicit UI path for `hintForQuestionId`
- [ ] Validate refusal behavior against real provider outputs
- [ ] Define token usage contract (non-null reporting when provider supports it)
- [ ] Keep ownership boundaries: tutor still cannot mutate progress/grades/records

---

## Intentional Deferrals

Deferred to later phases/sections:

- Richer memory retrieval and cross-session learner memory
- Multi-turn chat UI transcript and advanced session UX
- Teacher/parent AI workflows and summaries
- Broader personalization engines (`LearnerProfile`, `TopicMasterySnapshot` usage)
- Assignment-aware tutoring and intervention workflows
- Recommendation logic and advanced analytics
- Production-grade LLM operations (streaming, observability, moderation pipelines)

Permanently out of MVP scope:

- Open-domain chat outside current lesson
- Autonomous academic record mutation

---

## Validation Verdict

### Is Section 11 complete enough to move forward?

Yes. Section 11 is complete and coherent for Phase 1 scope.

### Maintainability review

- Strong: boundaries are codified in both type contracts and dedicated prompt module
- Strong: guardrail strings are centralized and test-anchored
- Medium risk: backend sends full history; prompt builder caps usage but payload size can drift over long sessions

### Safety and privacy review

- Strong: `CorrectAnswer` exclusion is enforced at data selection boundary
- Strong: lesson-only and direct-answer refusal are explicit and tested
- Strong: first-name-only student personalization in AI payloads

### Future extensibility review

- Strong: provider abstraction allows swapping stub for real provider without controller/service redesign
- Strong: prompt builder is a single extension point for future RAG/profile context blocks
- Medium: token usage contract is currently placeholder (`tokens_used = null`)

### Must-fix before the next section

- No critical blocker remains for moving forward.

Recommended early next-step fixes (non-blocking):

1. Add backend-side history cap before serialization.
2. Expose `hintForQuestionId` in TutorPanel UX.
3. Introduce non-null token usage reporting once real provider is wired.

### Key risks/tradeoffs

- Prompt-only guardrails are practical and testable, but real-provider behavior drift still needs empirical validation in Phase 2.
- Narrow MVP UI preserves momentum and safety now, at the cost of limited conversational affordances.
