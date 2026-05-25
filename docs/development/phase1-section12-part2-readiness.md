# Phase 1 — Section 12, Part 2 Readiness

## Goal

Define and lightly scaffold lesson ingestion/indexing boundaries so future tutor
retrieval can use derived artifacts while canonical lesson content remains
unchanged.

---

## Completed in Part 2

- [x] Added explicit ingestion/indexing event model (`created`, `updated`, `unpublished`, `deleted`)
- [x] Added indexing orchestrator (`LessonRetrievalIndexer`)
- [x] Added retrieval query boundary (`LessonScopedRetriever`)
- [x] Added embedding provider boundary interface (`EmbeddingProvider`)
- [x] Added derived artifact repository boundary (`RetrievalArtifactRepository`)
- [x] Added in-memory repository scaffold for local behavior validation
- [x] Added hash-based skip re-index behavior for unchanged lesson content
- [x] Added artifact removal behavior for unpublished/deleted lessons
- [x] Added tests for lifecycle and retrieval boundary behavior
- [x] Documented pipeline boundaries and deferred production concerns

---

## Why this is enough now

- Keeps implementation explicit and lesson-scoped
- Avoids coupling tutor runtime to unfinished vector infrastructure
- Introduces minimal interfaces needed for provider/store swap later
- Prevents canonical schema churn

---

## Non-goals preserved

- No production vector DB integration
- No production embedding provider integration
- No async job system rollout
- No generic enterprise search platform

---

## Suggested next step (Part 3+)

1. Wire event trigger points from backend lesson create/update/delete flows
2. Add one concrete embedding adapter (provider-backed)
3. Add one concrete artifact store adapter (vector/index backend)
4. Keep retrieval API lesson-scoped by default
