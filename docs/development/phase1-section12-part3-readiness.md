# Phase 1 — Section 12, Part 3 Readiness

## Goal

Define and scaffold the tutor retrieval-time path from student question to
lesson-scoped retrieved context.

---

## Completed in Part 3

- [x] Added tutor retrieval query DTO and intent hint modeling
- [x] Added lesson-first retrieval strategy boundary and concrete scaffold
- [x] Added deterministic section filtering in retrieval-time selection
- [x] Implemented conservative first expansion rule: only same-chapter neighboring lessons (opt-in)
- [x] Added compact context assembly service (`TutorRetrievalContextAssembler`)
- [x] Added retrieval context render helper for future prompt integration
- [x] Added unit tests for intent inference, lesson-scope query build, section filtering, and context assembly
- [x] Preserved explicit no-open-domain retrieval boundary

---

## Why this is enough now

- Connects ingestion artifacts (Part 2) to tutor runtime context assembly
- Keeps retrieval narrow and curriculum grounded
- Avoids premature complexity in ranking and memory retrieval
- Maintains provider/storage independence through explicit boundaries

---

## Guardrails preserved

- Lesson-first retrieval by default
- Scope expansion is tightly constrained to same-chapter neighbors only
- Compact retrieved context to reduce noise and drift

---

## Suggested next step (Part 4+)

1. Wire assembled retrieval context into tutor prompt composition path
2. Add one close-curriculum expansion strategy (chapter-neighbor only)
3. Add retrieval quality checks and minimal trace logging for debugging
4. Keep hard boundary against open-domain retrieval
