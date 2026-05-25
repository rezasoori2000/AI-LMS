# Tutor Retrieval Query and Context Assembly (Section 12, Part 3)

## Purpose

Define how a tutor request becomes a narrow internal retrieval query and how
retrieved lesson artifacts are assembled into a grounded tutoring context.

This part focuses on lesson-scoped retrieval-time behavior, not advanced search
optimization or final prompt engineering.

---

## Request-to-Retrieval Flow

1. Tutor request arrives with:
   - `TutorContextSnapshot` (lesson, subject, grade, progress, history)
   - student question text
2. Build `TutorRetrievalQuery`:
   - `lesson_id` (required)
   - `question_text`
   - optional `section_scope`
   - inferred or explicit `intent_hint` (`explain`, `simplify`, `example`, `clarify`)
3. Execute lesson-first retrieval strategy:
   - query lesson artifacts only
   - apply deterministic section filter when provided
   - limit to small `top_k` set
4. Assemble `TutorRagContext`:
   - selected retrieval items
   - lesson/progress state
   - notes when retrieval signal is weak

---

## Scaffolding Implemented

`ai-service/app/services/tutor_retrieval_context.py`

- Query DTO: `TutorRetrievalQuery`
- Result DTO: `TutorRetrievalItem`
- Context DTO: `TutorRagContext`
- Strategy boundary: `TutorRetrievalStrategy`
- Concrete strategy: `LessonFirstRetrievalStrategy`
- Context assembly service: `TutorRetrievalContextAssembler`
- Future prompt helper: `format_retrieval_context_block(...)`
- Intent inference helper: `infer_intent_hint(...)`

Compatibility:

- Works with Part 2 retrieval pipeline boundaries (`LessonScopedRetriever`)
- Uses existing `TutorContextSnapshot` from current tutor flow
- Keeps learner-memory integration deferred and decoupled

---

## Scope Expansion Rules

Default behavior:

- Current lesson scope only

First expansion rule (implemented, conservative):

- If lesson scope returns no meaningful signal, allow expansion only to
   explicitly provided **same-chapter neighboring lesson IDs**.
- Expansion is opt-in (`allow_curriculum_expansion=True`) and requires
   `chapter_neighbor_lesson_ids` in the query.

Not allowed in this version:

- Same-subject same-grade broadening
- Cross-curriculum expansion
- Open-domain retrieval

Safety rationale:

- Keeps tutor grounded in explicit curriculum context
- Avoids broad uncontrolled retrieval that can produce drift

---

## Context Assembly Rules

- Keep context compact (`max_items` default small)
- Prefer high-signal lesson chunks over large noisy payloads
- Preserve progress state (`NotStarted`, `InProgress`, `Completed`) in context
- Record notes when retrieval signal is weak so tutor can answer conservatively

---

## Deferred

- Advanced reranking and retrieval evaluation
- Rich topic graph and prerequisite expansion logic
- Multi-turn memory retrieval integration
- Provider-specific prompt optimization
- Cross-curriculum/global retrieval
