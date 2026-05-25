# Retrieval-Ready Lesson Architecture (Section 12, Part 1)

## Purpose

This document defines the minimum internal content architecture needed so lesson
content can later support tutor-led retrieval (RAG) without redesigning the
whole content system.

This part does not introduce production vector infrastructure.

---

## Current State Review

Current canonical lesson model:

- `Lesson.Content` stores lesson text as Markdown (`text` in PostgreSQL)
- Lesson ownership/scope already exists through chapter/subject/grade relations
- Admin authoring and student lesson playback already depend on this field

Conclusion:

- The current model is acceptable as canonical source content for Phase 1.
- It is not fully retrieval-ready by itself because chunk boundaries and
  retrieval metadata are implicit.
- Minimal refinement should be derived-artifact preparation, not canonical
  schema duplication.

---

## Canonical vs Render vs Retrieval Artifacts

### Canonical (store now)

- `Lesson` row and its existing relational context
- `Lesson.Content` Markdown text
- Existing curriculum hierarchy (`Grade` → `Subject` → `Chapter` → `Lesson`)

### Render-oriented (derived at read-time)

- UI presentation blocks for student lesson view
- Heading display structure, paragraph flow, and any visual formatting

### Retrieval-oriented (derived artifacts, not canonical)

- Normalized lesson text
- Stable section blocks based on Markdown heading boundaries
- Chunk artifacts scoped to lesson metadata
- Content hash and content format/version metadata used for idempotent re-index

Rule:

- Embeddings and index documents are re-creatable derived artifacts and must not
  become the source of truth for lesson content.

---

## Minimal Structural Refinements Implemented

Implemented in ai-service:

- `app/services/retrieval_prep.py`
  - `normalize_lesson_markdown(content)`
  - `split_into_section_blocks(content)`
  - `build_lesson_chunks(...)`

What this adds:

- Stable section boundaries (H2+ headings + overview block)
- Deterministic chunk IDs (`lesson:bX:cY`)
- Chunk metadata required for future scoped retrieval:
  - `tenant_id`, `chapter_id`, `lesson_id`, `lesson_title`
  - `subject_name`, `grade_name`
  - `block_index`, `block_title`, `block_heading_level`
  - `content_format` (`markdown_v1`)
  - `content_hash` (sha256 of normalized content)

No persistence schema changes were introduced.

---

## Internal RAG Flow (Future, Foundation-aligned)

Planned flow for lesson-scoped retrieval:

1. Canonical read
2. Normalize (`normalize_lesson_markdown`)
3. Block split (`split_into_section_blocks`)
4. Chunk build (`build_lesson_chunks`)
5. Embed chunks (deferred)
6. Index chunks (deferred)
7. Retrieve by strict scope filters (lesson-first, then chapter/subject)

Safety principle:

- Tutor runtime should prefer lesson-scoped retrieval first; broad fallback search
  across unrelated content is out of scope for this phase.

---

## Compatibility Notes

This design preserves current behavior:

- Admin content CRUD remains unchanged
- Student lesson rendering remains unchanged
- Progress tracking remains unchanged
- Tutor context assembly remains unchanged

The new retrieval-prep module is additive and internal.

---

## Deferred to Later RAG Sections

- Embedding provider integration
- Vector database/index infrastructure
- Re-ranking and hybrid retrieval
- Cross-lesson/global knowledge retrieval
- Multi-turn memory retrieval and long-horizon learner memory
- Operational indexing pipeline (queue/workers/backfill)
