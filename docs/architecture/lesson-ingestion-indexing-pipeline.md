# Lesson Ingestion and Internal Indexing Pipeline (Section 12, Part 2)

## Purpose

Define a narrow internal pipeline that converts canonical lesson content into
derived retrieval artifacts for future tutor RAG usage.

This is an internal scaffold, not production retrieval infrastructure.

---

## Pipeline Boundary

Canonical source (unchanged):

- `Lesson.Content` and curriculum relations in backend domain model

Derived retrieval side (new internal boundary):

- Normalize lesson content
- Split into deterministic section/chunk boundaries
- Build retrieval metadata and stable chunk IDs
- Optionally generate embeddings (provider boundary only)
- Store retrieval artifacts separately from canonical lesson rows

---

## Ingestion / Indexing Flow

1. Receive `LessonIngestionInput` event (`created` / `updated` / `unpublished` / `deleted`)
2. If event is `unpublished` or `deleted`: remove derived artifacts for lesson
3. Else:
   - Build chunks via `build_lesson_chunks(...)`
   - Read `content_hash`
   - Compare against repository hash for lesson
   - If unchanged: skip re-index
   - If changed: replace lesson artifacts atomically
4. Retrieval query uses strict lesson scope (`lesson_id`) and returns ranked candidates

---

## Interfaces and Scaffolding

Implemented in `ai-service/app/services/retrieval_pipeline.py`:

- `LessonRetrievalIndexer`
- `LessonScopedRetriever`
- `EmbeddingProvider` (boundary interface)
- `RetrievalArtifactRepository` (boundary interface)
- `InMemoryRetrievalArtifactRepository` (local scaffold)
- `NoOpEmbeddingProvider` (local scaffold)

Key DTO/dataclass models:

- `LessonIngestionInput`
- `RetrievalArtifact`
- `RetrievalQuery`
- `RetrievalCandidate`
- `IndexingResult`

---

## Re-index Lifecycle Rules

### Lesson created

- Index immediately (synchronous scaffold)

### Lesson updated

- Recompute content hash
- Skip re-index if hash unchanged
- Replace artifacts if hash changed

### Lesson unpublished

- Remove all derived retrieval artifacts for lesson

### Lesson deleted

- Remove all derived retrieval artifacts for lesson

---

## Separation Rules

- Canonical lesson records remain source of truth
- Derived artifacts are replaceable and re-creatable
- Tutor runtime should query derived artifacts only through retrieval boundary
- Tutor runtime should not depend on internal indexing implementation details

---

## Compatibility

Current behavior remains unchanged:

- Admin lesson authoring CRUD
- Student lesson consumption
- Progress tracking
- Tutor context assembly

---

## Deferred

- Production vector DB integration
- Production embedding provider
- Async indexing workers/queues
- Hybrid retrieval and reranking
- Cross-curriculum retrieval/search
- Multi-turn memory retrieval
