# Retrieval and RAG Foundations (Section 12, Part 5)

## Purpose

This document captures the minimum retrieval/RAG boundaries that make tutor-led
teaching feasible in Phase 1 without turning the platform into a production
retrieval system.

It is a validation and closeout note, not a production design.

---

## What Section 12 Established

- Canonical lesson content remains in the domain model as the source of truth
- Retrieval artifacts are derived from lesson content and are replaceable
- Lesson ingestion is deterministic and lesson-scoped
- Tutor retrieval is lesson-first, with only conservative same-chapter neighbor expansion
- Tutor interaction refinement is explicit, narrow, and metadata-driven

This combination is enough to support future tutor-led teaching work without
committing to broad search or persistent learner intelligence yet.

---

## Architecture Boundaries

### Canonical content

- `Lesson.Content` stays as the editable lesson source material
- Curriculum relations define the primary scope of each lesson

### Derived retrieval artifacts

- Chunking, section blocks, and content hashes are derived
- Retrieval indexes and embeddings are replaceable implementation details
- Derived artifacts must not become the content source of truth

### Tutor runtime retrieval

- Retrieval starts from the current lesson
- Same-chapter neighbors are the only allowed first expansion path in this phase
- Retrieval context stays compact and explanation-oriented

### Interaction refinement

- The tutor can simplify, explain more deeply, give examples, or clarify confusion
- These are response-shaping controls, not autonomous tutoring policies

---

## Validation Summary

The current foundation is coherent enough to continue:

- The content model, ingestion pipeline, retrieval query design, and tutor context assembly line up
- Guardrails still keep the tutor lesson-grounded instead of open-domain
- The interaction policy improves tutoring style without broadening capability scope
- The AI service tests cover the retrieval prep, pipeline, context assembly, and interaction policy surfaces

Practical conclusion:

- Section 12 is complete enough to move forward with later tutoring work
- It is not yet a production RAG system

---

## Intentionally Deferred

- Production vector database operations
- Hybrid retrieval and reranking
- Broad cross-curriculum retrieval
- Long-term learner memory retrieval
- Multimodal STT/TTS runtime
- Adaptive pedagogical sequencing
- Recommendation and intervention systems
- Advanced evaluation platforms

---

## Maintainability Notes

- Keep canonical content and derived retrieval artifacts separate
- Keep expansion rules explicit and small
- Avoid hiding retrieval behavior inside prompt text alone
- Prefer lesson-scoped safeguards over broad fallback heuristics
