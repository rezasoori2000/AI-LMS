# Phase 1 — Section 12, Part 1 Readiness

## Goal

Establish retrieval-ready lesson architecture foundations for tutor-led teaching
without building the full production RAG system.

---

## Completed in Part 1

- [x] Reviewed current lesson model (`Lesson.Content` Markdown) for retrieval readiness
- [x] Defined canonical vs render-oriented vs retrieval-derived boundaries
- [x] Added internal retrieval preparation module in ai-service:
  - [x] text normalization
  - [x] deterministic section/block splitting
  - [x] deterministic chunk artifact creation with lesson-scoped metadata
- [x] Added unit tests for retrieval preparation behavior
- [x] Documented architecture and deferred boundaries

---

## Why this is enough now

- Keeps canonical source of truth unchanged (no schema churn)
- Adds stable chunking boundaries for future tutor retrieval
- Introduces metadata needed for safe lesson-scoped retrieval
- Avoids premature vector platform lock-in

---

## Verified constraints

- No production vector DB integration added
- No embedding provider integration added
- No broad search platform redesign
- No tutor runtime redesign
- No impact on admin/student lesson flows

---

## Next-section handoff (suggested)

1. Add indexing orchestration contract (when to regenerate artifacts on lesson update)
2. Add pluggable embedding adapter (provider-agnostic)
3. Add lesson-scoped retrieval API contract for tutor runtime
4. Keep strict filter policy: lesson-first retrieval before any broader fallback
