# Personalization Readiness

_Phase 1, Section 9 — established 2026-05-20. Updated when new observation data or boundary decisions are made._

This document describes the boundaries between the current Phase 1 persistence model and the planned Phase 3 learner personalization layer.  
It is the authoritative reference for any contributor implementing the Phase 3 AI tutor or `LearnerProfile` feature.

---

## What is already built (Phase 1 observation layer)

These tables are the **read-only source data** for any future personalization work.  
Do not modify them from personalization code. Do not add adaptive or behavioral fields to them.

| Entity | Table | What it records |
|--------|-------|----------------|
| `LessonProgress` | `lesson_progress` | Completion status and score per student per lesson |
| `QuestionAnswerRecord` | `question_answer_records` | Binary correct/incorrect outcome per question per lesson completion |
| `Enrollment` | `enrollments` | Which subjects a student is actively enrolled in |
| `AiConversation` / `AiMessage` | `ai_conversations` / `ai_messages` | AI tutor conversation history |
| `StudentProfile` | `student_profiles` | Student identity anchor; grade level; parent link |

These five surfaces give Phase 3 everything it needs to answer the core personalization questions — without additional data capture.

---

## What is reserved but not yet implemented (Phase 3)

All Phase 3 personalization entities belong in `LMS.Domain/Personalization/`.  
The namespace is reserved. See `DesignNotes.cs` in that folder for full entity shapes and boundary rules.

### Three-layer model

```
Layer A — Explicit Preferences  (student-set, revocable)
  LearnerPreferences     keyed by LearnerProfile.Id
        ↓ reads from (never writes back to)
Layer B — Derived Academic Signals  (computed, periodically refreshed)
  TopicMasterySnapshot   per (LearnerProfileId, SubjectId)
  ConsistencySignal      per LearnerProfileId + rolling window
        ↓ sourced from (read-only Phase 1 tables)
Layer C — Observation Layer  (Phase 1, immutable from Phase 3)
  LessonProgress · QuestionAnswerRecord · Enrollment
  AiConversation · AiMessage
```

### Entity summary

| Entity | Layer | Purpose |
|--------|-------|---------|
| `LearnerProfile` | Root | Navigation + deletion anchor; keyed by `(UserId, TenantId)` |
| `LearnerPreferences` | A | Explicit student-set preferences (explanation style, pacing, help-seeking) |
| `TopicMasterySnapshot` | B | Materialized correctness ratio per subject, sourced from `QuestionAnswerRecord` |
| `ConsistencySignal` | B | Rolling-window lesson engagement count, sourced from `LessonProgress` |

---

## Boundary rules

These rules prevent future Phase 3 work from creating coupling that breaks Phase 1 stability.

1. **`StudentProfile` and `User` carry zero adaptive or behavioral fields.**  
   Adding such fields is a breaking change that requires a Phase 3 review.

2. **The observation layer is read-only from Phase 3.**  
   `LessonProgress`, `QuestionAnswerRecord`, `Enrollment`, `AiConversation`, and `AiMessage` must never be written by personalization code. They are source-of-truth academic records.

3. **`LearnerPreferences` is the only personalization entity the student directly edits.**  
   All other Phase 3 entities are computed or AI-managed. `LearnerPreferences` fields must never be auto-populated from inferred signals.

4. **`LearnerProfile` is keyed by `User.Id`, not `StudentProfile.Id`.**  
   `User.Id` is the durable long-term identity. Phase 3 context-assembly code must resolve `StudentProfile.UserId` when joining to observation tables (which are keyed by `StudentProfile.Id`).

5. **`ConsistencySignal` is a factual activity count, not a gamification score.**  
   The word "streak" must not appear in entity names, API responses, or UI copy.

6. **`TopicMasterySnapshot` stores numeric ratios, not inferred labels.**  
   Do not add string fields like `RecurringWeakArea`. Surface the ratio; let the application layer interpret it.

---

## What Phase 3 AI context assembly will read

The AI tutor context-assembly service (Phase 3) reads the following to compose a student context:

| Signal | Source |
|--------|--------|
| What subject areas does the student struggle with? | `TopicMasterySnapshot.MasteryPercent` by subject |
| How does the student prefer to receive help? | `LearnerPreferences.ExplanationStyle`, `HelpSeekingStyle` |
| How consistently has the student been engaging? | `ConsistencySignal.ActiveDays` / `LessonsCompleted` |
| What questions has the student answered recently? | `QuestionAnswerRecord` (last N, ordered by `AnsweredAt`) |
| What has the AI tutor discussed before? | `AiConversation.Messages` |
| What grade and subjects is the student in? | `StudentProfile.GradeId`, `Enrollment` (active) |

No psychometric inference, no behavioral telemetry, and no external data sources are required.

---

## What is permanently excluded

Do not add these to any entity in `LMS.Domain/Personalization/` or anywhere else:

- `TypicalStudyTimeOfDay` — behavioral telemetry; raises surveillance concerns
- `AiMemoryStoreRef` on a domain entity — Qdrant/vector-store references are infrastructure, not domain
- Inferred concept-weakness string labels — derive signal from numeric ratios only
- Personality or cognitive-style classifications (MBTI, VARK, etc.)
- Health, disability, or mental-health inferences
- Engagement-addiction metrics (streak counters, loss-aversion scoring)
- General ability or IQ-style ranking

---

## GDPR / data-erasure scope

When a student's data is deleted, the `StudentDataDeletionService` (Phase 3) must process these tables in FK-safe order:

```
QuestionAnswerRecord
LessonProgress
AiMessage
AiConversation
ConsistencySignal          ← Phase 3
TopicMasterySnapshot       ← Phase 3
LearnerPreferences         ← Phase 3
LearnerProfile             ← Phase 3
Enrollment
TeacherStudentAssignment
StudentProfile
```

---

## Deferred items (not Phase 1)

| Item | Notes |
|------|-------|
| `LearnerProfile` entity + EF config + migration | Phase 3 implementation sprint |
| `LearnerPreferences` student-facing settings page | Needs UX design before backend work |
| `TopicMasterySnapshot` refresh in `CompleteLessonAsync` | Phase 3; inline first, background job later |
| `ConsistencySignal` computation service | Depends on `LearnerProfile` landing first |
| `Question.TopicId` for finer-grained mastery tagging | Separate Phase 3 content-taxonomy sub-task |
| Accessibility preference flags | UX/WCAG requirements must drive the model |
| Teacher observation notes on a student | Separate privacy/visibility design required |
| AI context-assembly service | Phase 3+; reads `LearnerProfile.*` at the application layer |
| Vector store integration | Phase 3+; AI tutor semantic search enhancement |
